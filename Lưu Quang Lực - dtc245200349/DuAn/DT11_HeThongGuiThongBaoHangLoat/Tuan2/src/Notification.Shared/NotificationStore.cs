using System.Data;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Notification.Shared;

public sealed class NotificationStore(NpgsqlDataSource source, ILogger<NotificationStore> logger)
{
    private static NpgsqlCommand Command(NpgsqlConnection connection, NpgsqlTransaction? transaction,
        string sql, params (string Name, object? Value)[] parameters)
    {
        var command = new NpgsqlCommand(sql, connection, transaction);
        foreach (var (name, value) in parameters) command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        return command;
    }

    public async Task InitializeAsync(CancellationToken ct)
    {
        using var resource = typeof(NotificationStore).Assembly.GetManifestResourceStream("Notification.Shared.schema.sql")!;
        using var reader = new StreamReader(resource);
        var sql = await reader.ReadToEndAsync(ct);
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await using var connection = await source.OpenConnectionAsync(ct);
                await using var tx = await connection.BeginTransactionAsync(ct);
                await using var schema = Command(connection, tx, "SELECT pg_advisory_xact_lock(110011); " + sql);
                await schema.ExecuteNonQueryAsync(ct);
                await tx.CommitAsync(ct);
                return;
            }
            catch (Exception ex) when (attempt < 30 && !ct.IsCancellationRequested)
            {
                logger.LogWarning("Database chưa sẵn sàng, lần {Attempt}: {ErrorType}", attempt, ex.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
        }
    }

    public async Task<bool> IsHealthyAsync(CancellationToken ct)
    {
        await using var command = source.CreateCommand("SELECT 1");
        return (int)(await command.ExecuteScalarAsync(ct))! == 1;
    }

    public async Task<Guid> CreateAsync(CreateCampaign input, CancellationToken ct)
    {
        var id = Guid.NewGuid();
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using (var create = Command(connection, tx,
            "INSERT INTO campaigns(id,name,subject,body) VALUES (@id,@name,@subject,@body)",
            ("id", id), ("name", input.Name), ("subject", input.Subject), ("body", input.Body)))
            await create.ExecuteNonQueryAsync(ct);
        // Một transaction, danh sách đã kiểm tra giới hạn 5.000 trước khi đến đây.
        await using (var insert = Command(connection, tx, """
            INSERT INTO deliveries(id,campaign_id,address)
            SELECT unnest(@ids), @campaign, unnest(@addresses)
            """, ("ids", input.Recipients.Select(_ => Guid.NewGuid()).ToArray()),
            ("campaign", id), ("addresses", input.Recipients)))
            await insert.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return id;
    }

    private const string SummarySql = """
        SELECT c.id,c.name,c.subject,c.body,
          CASE WHEN c.started_at IS NULL THEN 'Draft'
               WHEN count(*) FILTER (WHERE d.status IN ('Pending','Processing','RetryScheduled')) > 0 THEN 'Running'
               WHEN count(*) FILTER (WHERE d.status = 'Failed') > 0 THEN 'CompletedWithErrors'
               ELSE 'Completed' END AS status,
          c.created_at,c.started_at,count(d.id),
          count(*) FILTER (WHERE d.status IN ('Pending','Processing','RetryScheduled')),
          count(*) FILTER (WHERE d.status = 'Sent'),count(*) FILTER (WHERE d.status = 'Failed')
        FROM campaigns c LEFT JOIN deliveries d ON d.campaign_id=c.id
        """;

    private static CampaignSummary ReadCampaign(NpgsqlDataReader r) => new(r.GetGuid(0), r.GetString(1),
        r.GetString(2), r.GetString(3), r.GetString(4), r.GetDateTime(5),
        r.IsDBNull(6) ? null : r.GetDateTime(6), r.GetInt64(7), r.GetInt64(8), r.GetInt64(9), r.GetInt64(10));

    public async Task<List<CampaignSummary>> ListAsync(int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var command = Command(connection, null, SummarySql +
            " GROUP BY c.id ORDER BY c.created_at DESC,c.id LIMIT @limit OFFSET @offset",
            ("limit", pageSize), ("offset", (long)(page - 1) * pageSize));
        await using var reader = await command.ExecuteReaderAsync(ct);
        var result = new List<CampaignSummary>();
        while (await reader.ReadAsync(ct)) result.Add(ReadCampaign(reader));
        return result;
    }

    public async Task<CampaignSummary?> GetAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var command = Command(connection, null, SummarySql + " WHERE c.id=@id GROUP BY c.id", ("id", id));
        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ReadCampaign(reader) : null;
    }

    public async Task<bool> StartAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using var read = Command(connection, tx, "SELECT started_at FROM campaigns WHERE id=@id FOR UPDATE", ("id", id));
        var started = await read.ExecuteScalarAsync(ct);
        if (started is null) return false;
        if (started is DBNull)
        {
            await using var update = Command(connection, tx, """
                UPDATE campaigns SET started_at=now() WHERE id=@id;
                INSERT INTO outbox(id,delivery_id,campaign_id,generation)
                SELECT gen_random_uuid(),id,campaign_id,generation FROM deliveries WHERE campaign_id=@id;
                """, ("id", id));
            await update.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<int?> RetryFailedAsync(Guid id, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using var exists = Command(connection, tx, "SELECT id FROM campaigns WHERE id=@id FOR UPDATE", ("id", id));
        if (await exists.ExecuteScalarAsync(ct) is null) return null;
        await using var retry = Command(connection, tx, """
            WITH retried AS (
                UPDATE deliveries SET status='Pending',generation=generation+1,attempt_count=0,
                  next_attempt_at=NULL,lease_until=NULL,owner=NULL,last_error=NULL
                WHERE campaign_id=@id AND status='Failed'
                RETURNING id,campaign_id,generation
            )
            INSERT INTO outbox(id,delivery_id,campaign_id,generation)
            SELECT gen_random_uuid(),id,campaign_id,generation FROM retried
            """, ("id", id));
        var count = await retry.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return count;
    }

    public async Task<List<DeliveryView>> DeliveriesAsync(Guid campaignId, string? status, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        var filter = status switch
        {
            "Pending" => " AND status IN ('Pending','Processing','RetryScheduled')",
            "Sent" => " AND status='Sent'",
            "Failed" => " AND status='Failed'",
            _ => ""
        };
        await using var command = Command(connection, null, """
            SELECT id,address,status,attempt_count,total_attempts,next_attempt_at,sent_at,last_error
            FROM deliveries WHERE campaign_id=@id
            """ + filter + " ORDER BY address,id LIMIT @limit OFFSET @offset",
            ("id", campaignId), ("limit", pageSize), ("offset", (long)(page - 1) * pageSize));
        await using var r = await command.ExecuteReaderAsync(ct);
        var items = new List<DeliveryView>();
        while (await r.ReadAsync(ct)) items.Add(new(r.GetGuid(0), r.GetString(1), r.GetString(2), r.GetInt32(3),
            r.GetInt32(4), r.IsDBNull(5) ? null : r.GetDateTime(5), r.IsDBNull(6) ? null : r.GetDateTime(6),
            r.IsDBNull(7) ? null : r.GetString(7)));
        return items;
    }

    public async Task<List<AttemptView>> AttemptsAsync(Guid campaignId, Guid deliveryId, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var command = Command(connection, null, """
            SELECT a.generation,a.worker_id,a.started_at,a.finished_at,a.outcome,a.error
            FROM delivery_attempts a JOIN deliveries d ON d.id=a.delivery_id
            WHERE d.campaign_id=@campaign AND d.id=@delivery ORDER BY a.generation DESC LIMIT 100
            """, ("campaign", campaignId), ("delivery", deliveryId));
        await using var r = await command.ExecuteReaderAsync(ct);
        var items = new List<AttemptView>();
        while (await r.ReadAsync(ct)) items.Add(new(r.GetInt32(0), r.GetString(1), r.GetDateTime(2),
            r.IsDBNull(3) ? null : r.GetDateTime(3), r.GetString(4), r.IsDBNull(5) ? null : r.GetString(5)));
        return items;
    }

    public async Task<bool> PublishNextAsync(Func<SendNotification, Task> publish, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using var command = Command(connection, tx, """
            SELECT id,campaign_id,delivery_id,generation FROM outbox
            WHERE published_at IS NULL ORDER BY created_at,id LIMIT 1 FOR UPDATE SKIP LOCKED
            """);
        SendNotification job;
        await using (var r = await command.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return false;
            job = new(r.GetGuid(0), r.GetGuid(1), r.GetGuid(2), r.GetInt32(3));
        }
        await publish(job);
        await using var mark = Command(connection, tx, "UPDATE outbox SET published_at=now() WHERE id=@id", ("id", job.MessageId));
        await mark.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<ClaimedDelivery?> ClaimAsync(SendNotification job, string workerId, CancellationToken ct)
    {
        var owner = Guid.NewGuid();
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using var claim = Command(connection, tx, """
            UPDATE deliveries d SET status='Processing',owner=@owner,lease_until=now()+interval '60 seconds',
              attempt_count=attempt_count+1,total_attempts=total_attempts+1
            FROM campaigns c WHERE d.id=@id AND d.campaign_id=@campaign AND c.id=d.campaign_id
              AND c.started_at IS NOT NULL AND d.generation=@generation AND d.status='Pending'
            RETURNING d.address,c.subject,c.body,d.attempt_count
            """, ("owner", owner), ("id", job.DeliveryId), ("campaign", job.CampaignId), ("generation", job.Generation));
        ClaimedDelivery delivery;
        await using (var r = await claim.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return null;
            delivery = new(job.DeliveryId, job.CampaignId, job.Generation, owner,
                r.GetString(0), r.GetString(1), r.GetString(2), r.GetInt32(3));
        }
        await using var attempt = Command(connection, tx, """
            INSERT INTO delivery_attempts(delivery_id,generation,worker_id) VALUES (@id,@generation,@worker)
            """, ("id", job.DeliveryId), ("generation", job.Generation), ("worker", workerId));
        await attempt.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return delivery;
    }

    public async Task CompleteAsync(ClaimedDelivery delivery, string? error, bool permanent, int maxAttempts, CancellationToken ct)
    {
        var status = error is null ? "Sent" : CampaignRules.ShouldRetry(permanent, delivery.AttemptCount, maxAttempts)
            ? "RetryScheduled" : "Failed";
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using var update = Command(connection, tx, """
            UPDATE deliveries SET status=@status,last_error=@error,owner=NULL,lease_until=NULL,
              sent_at=CASE WHEN @status='Sent' THEN now() ELSE NULL END,
              next_attempt_at=CASE WHEN @status='RetryScheduled' THEN now()+@delay ELSE NULL END
            WHERE id=@id AND generation=@generation AND owner=@owner AND status='Processing'
            """, ("status", status), ("error", error), ("delay", CampaignRules.RetryDelay(delivery.AttemptCount)),
            ("id", delivery.Id), ("generation", delivery.Generation), ("owner", delivery.Owner));
        if (await update.ExecuteNonQueryAsync(ct) != 1)
            throw new InvalidOperationException("Delivery lease đã được thay thế; không ghi đè kết quả.");
        await using var history = Command(connection, tx, """
            UPDATE delivery_attempts SET finished_at=now(),outcome=@status,error=@error
            WHERE delivery_id=@id AND generation=@generation
            """, ("id", delivery.Id), ("generation", delivery.Generation), ("status", status), ("error", error));
        await history.ExecuteNonQueryAsync(ct);
        await AppendAuditAsync(connection, tx, delivery.CampaignId, delivery.Id, delivery.Generation, status, ct);
        await tx.CommitAsync(ct);
    }

    public async Task<bool> RecoverNextAsync(int maxAttempts, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using var read = Command(connection, tx, """
            SELECT id,campaign_id,generation,status,attempt_count FROM deliveries
            WHERE (status='RetryScheduled' AND next_attempt_at<=now())
               OR (status='Processing' AND lease_until<=now())
            ORDER BY COALESCE(next_attempt_at,lease_until),id LIMIT 1 FOR UPDATE SKIP LOCKED
            """);
        Guid id, campaign;
        int generation, attempts;
        string oldStatus;
        await using (var r = await read.ExecuteReaderAsync(ct))
        {
            if (!await r.ReadAsync(ct)) return false;
            id = r.GetGuid(0); campaign = r.GetGuid(1); generation = r.GetInt32(2);
            oldStatus = r.GetString(3); attempts = r.GetInt32(4);
        }
        var exhausted = attempts >= maxAttempts;
        if (oldStatus == "Processing")
        {
            await using var history = Command(connection, tx, """
                UPDATE delivery_attempts SET finished_at=now(),outcome='Interrupted',error='Worker lease expired; delivery outcome may be unknown.'
                WHERE delivery_id=@id AND generation=@generation AND finished_at IS NULL
                """, ("id", id), ("generation", generation));
            await history.ExecuteNonQueryAsync(ct);
            await AppendAuditAsync(connection, tx, campaign, id, generation, "Interrupted", ct);
        }
        await using var update = Command(connection, tx, """
            UPDATE deliveries SET status=@status,generation=generation+1,owner=NULL,lease_until=NULL,next_attempt_at=NULL,
              last_error=CASE WHEN @interrupted THEN 'Worker lease expired; delivery outcome may be unknown.' ELSE last_error END
            WHERE id=@id
            """, ("id", id), ("status", exhausted ? "Failed" : "Pending"), ("interrupted", oldStatus == "Processing"));
        await update.ExecuteNonQueryAsync(ct);
        if (!exhausted)
        {
            await using var enqueue = Command(connection, tx, """
                INSERT INTO outbox(id,delivery_id,campaign_id,generation) VALUES (@message,@id,@campaign,@generation)
                """, ("message", Guid.NewGuid()), ("id", id), ("campaign", campaign), ("generation", generation + 1));
            await enqueue.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
        logger.LogInformation("Recovered DeliveryId={DeliveryId} From={PreviousStatus} Exhausted={Exhausted}", id, oldStatus, exhausted);
        return true;
    }

    private static async Task AppendAuditAsync(NpgsqlConnection connection, NpgsqlTransaction tx, Guid campaign,
        Guid delivery, int generation, string outcome, CancellationToken ct)
    {
        await using var command = Command(connection, tx, """
            INSERT INTO audit_outbox(event_id,campaign_id,delivery_id,generation,outcome)
            VALUES (@event,@campaign,@delivery,@generation,@outcome)
            """, ("event", Guid.NewGuid()), ("campaign", campaign), ("delivery", delivery),
            ("generation", generation), ("outcome", outcome));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<bool> PublishNextAuditAsync(Func<DeliveryAuditEvent, Task> publish, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var tx = await connection.BeginTransactionAsync(ct);
        await using var command = Command(connection, tx, """
            SELECT event_id,campaign_id,delivery_id,generation,outcome,occurred_at
            FROM audit_outbox WHERE published_at IS NULL
            ORDER BY occurred_at,event_id LIMIT 1 FOR UPDATE SKIP LOCKED
            """);
        DeliveryAuditEvent item;
        await using (var reader = await command.ExecuteReaderAsync(ct))
        {
            if (!await reader.ReadAsync(ct)) return false;
            item = ReadAuditEvent(reader);
        }
        await publish(item);
        await using var mark = Command(connection, tx,
            "UPDATE audit_outbox SET published_at=now() WHERE event_id=@event", ("event", item.EventId));
        await mark.ExecuteNonQueryAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task RecordAuditAsync(DeliveryAuditEvent item, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var command = Command(connection, null, """
            INSERT INTO audit_events(event_id,campaign_id,delivery_id,generation,outcome,occurred_at)
            VALUES (@event,@campaign,@delivery,@generation,@outcome,@occurred)
            ON CONFLICT (event_id) DO NOTHING
            """, ("event", item.EventId), ("campaign", item.CampaignId), ("delivery", item.DeliveryId),
            ("generation", item.Generation), ("outcome", item.Outcome), ("occurred", item.OccurredAt));
        await command.ExecuteNonQueryAsync(ct);
    }

    public async Task<List<DeliveryAuditEvent>> AuditAsync(Guid campaignId, int page, int pageSize, CancellationToken ct)
    {
        await using var connection = await source.OpenConnectionAsync(ct);
        await using var command = Command(connection, null, """
            SELECT event_id,campaign_id,delivery_id,generation,outcome,occurred_at
            FROM audit_events WHERE campaign_id=@campaign
            ORDER BY occurred_at DESC,event_id LIMIT @limit OFFSET @offset
            """, ("campaign", campaignId), ("limit", pageSize), ("offset", (long)(page - 1) * pageSize));
        await using var reader = await command.ExecuteReaderAsync(ct);
        var items = new List<DeliveryAuditEvent>();
        while (await reader.ReadAsync(ct)) items.Add(ReadAuditEvent(reader));
        return items;
    }

    private static DeliveryAuditEvent ReadAuditEvent(NpgsqlDataReader reader) => new(
        reader.GetGuid(0), reader.GetGuid(1), reader.GetGuid(2), reader.GetInt32(3),
        reader.GetString(4), reader.GetDateTime(5));
}
