using System.Data;
using System.Reflection;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace Clinic.Core;

public sealed class Database(NpgsqlDataSource source)
{
    public ValueTask<NpgsqlConnection> Open(CancellationToken ct = default) => source.OpenConnectionAsync(ct);
    public static NpgsqlCommand Command(NpgsqlConnection c, string sql, params object?[] args)
    {
        var cmd = new NpgsqlCommand(sql, c);
        foreach (var arg in args) cmd.Parameters.Add(new NpgsqlParameter { Value = arg ?? DBNull.Value });
        return cmd;
    }
    public static async Task<int> Execute(NpgsqlConnection c, string sql, params object?[] args)
    {
        await using var cmd = Command(c, sql, args);
        return await cmd.ExecuteNonQueryAsync();
    }
    public static async Task<T?> Scalar<T>(NpgsqlConnection c, string sql, params object?[] args)
    {
        await using var cmd = Command(c, sql, args);
        var result = await cmd.ExecuteScalarAsync();
        return result is null or DBNull ? default : (T)result;
    }
    public static async Task<List<T>> Read<T>(NpgsqlConnection c, string sql, Func<NpgsqlDataReader,T> map, params object?[] args)
    {
        await using var cmd = Command(c, sql, args);
        await using var r = await cmd.ExecuteReaderAsync();
        List<T> result = [];
        while (await r.ReadAsync()) result.Add(map(r));
        return result;
    }
    public async Task Initialize()
    {
        await using var c = await Open();
        await using var tx = await c.BeginTransactionAsync();
        await Execute(c, "SELECT pg_advisory_xact_lock(90009)");
        await Execute(c, "CREATE TABLE IF NOT EXISTS schema_migrations(version text PRIMARY KEY, applied_at timestamptz NOT NULL DEFAULT now())");
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.EndsWith(".sql")).Order())
        {
            if (await Scalar<long>(c, "SELECT count(*) FROM schema_migrations WHERE version=$1", name) > 0) continue;
            using var reader = new StreamReader(assembly.GetManifestResourceStream(name)!);
            await Execute(c, await reader.ReadToEndAsync());
            await Execute(c, "INSERT INTO schema_migrations(version) VALUES($1)", name);
        }
        await Execute(c, "INSERT INTO doctors VALUES(1,'BS. Nguyễn Minh An','Nội tổng quát','P.101'),(2,'BS. Trần Thu Hà','Tim mạch','P.102') ON CONFLICT DO NOTHING");
        for (var d = 0; d < 7; d++)
        {
            var date = ClinicTime.Today.AddDays(d);
            foreach (var doctor in new[] { 1, 2 })
            foreach (var (start, end, capacity) in new[] { (0, 8, 1), (8, 12, 8), (12, 24, 8) })
            {
                var from = ClinicTime.At(date, new TimeOnly(start,0));
                var to = end == 24 ? ClinicTime.At(date.AddDays(1), TimeOnly.MinValue) : ClinicTime.At(date, new TimeOnly(end,0));
                await Execute(c, "INSERT INTO slots(id,doctor_id,visit_date,starts_at,ends_at,capacity) VALUES($1,$2,$3,$4,$5,$6) ON CONFLICT(doctor_id,starts_at) DO NOTHING", Guid.NewGuid(), doctor, date, from.ToUniversalTime(), to.ToUniversalTime(), capacity);
            }
        }
        await tx.CommitAsync();
    }
    public async Task<List<Doctor>> Doctors()
    {
        await using var c = await Open();
        return await Read(c,"SELECT id,name,specialty,room FROM doctors ORDER BY id",r=>new Doctor(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetString(3)));
    }
    public async Task<List<Slot>> Slots(int doctor, DateOnly date)
    {
        await using var c = await Open();
        return await Read(c,"SELECT id,doctor_id,visit_date,starts_at,ends_at,capacity,capacity-reserved FROM slots WHERE doctor_id=$1 AND visit_date=$2 AND ends_at>now() ORDER BY starts_at",r=>new Slot(r.GetGuid(0),r.GetInt32(1),r.GetFieldValue<DateOnly>(2),r.GetFieldValue<DateTimeOffset>(3),r.GetFieldValue<DateTimeOffset>(4),r.GetInt32(5),r.GetInt32(6)),doctor,date);
    }
    public static async Task LockQueue(NpgsqlConnection c, int doctor, DateOnly date)
    {
        await Execute(c,"INSERT INTO doctor_queues(doctor_id,visit_date) VALUES($1,$2) ON CONFLICT DO NOTHING",doctor,date);
        await Execute(c,"SELECT 1 FROM doctor_queues WHERE doctor_id=$1 AND visit_date=$2 FOR UPDATE",doctor,date);
    }
    public static Task<int> Enqueue(NpgsqlConnection c, string kind, Guid appointment, int doctor, DateOnly date)
    {
        var message = new BusMessage(Guid.NewGuid(),appointment,doctor,date);
        return Execute(c,"INSERT INTO outbox(id,kind,body) VALUES($1,$2,$3)",message.Id,kind,JsonSerializer.Serialize(message));
    }
    public async Task<(Guid Id,string Token)> Book(BookRequest request, string? idempotencyKey = null, string? signingKey = null)
    {
        if (string.IsNullOrWhiteSpace(request.PatientName) || request.PatientName.Length > 100 || string.IsNullOrWhiteSpace(request.Phone) || request.Phone.Length > 25)
            throw new RuleException(400,"Nhập họ tên (tối đa 100 ký tự) và số điện thoại (tối đa 25 ký tự).");
        request = request with { PatientName = request.PatientName.Trim(), Phone = request.Phone.Trim() };
        string? keyHash = null, requestHash = null, replayToken = null;
        if (idempotencyKey is not null)
        {
            if (idempotencyKey.Length != 64 || !idempotencyKey.All(char.IsAsciiHexDigit))
                throw new RuleException(400,"Idempotency-Key phải gồm 64 ký tự hex ngẫu nhiên.");
            if (string.IsNullOrEmpty(signingKey)) throw new InvalidOperationException("Missing booking signing key");
            keyHash = Secrets.Hash(idempotencyKey);
            requestHash = Secrets.Hash(JsonSerializer.Serialize(request));
            replayToken = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey),Encoding.UTF8.GetBytes("booking-access:" + idempotencyKey)));
        }
        await using var c = await Open();
        await using var tx = await c.BeginTransactionAsync(IsolationLevel.ReadCommitted);
        if (keyHash is not null)
        {
            // Serialize retries of the same request, including across API instances.
            await Execute(c,"SELECT pg_advisory_xact_lock(hashtextextended($1, 90010))",keyHash);
            var previous = (await Read(c,"SELECT b.appointment_id,b.request_hash,a.token_hash FROM booking_requests b JOIN appointments a ON a.id=b.appointment_id WHERE b.key_hash=$1",r=>(Id:r.GetGuid(0),Hash:r.GetString(1),TokenHash:r.GetString(2)),keyHash)).SingleOrDefault();
            if (previous.Id != Guid.Empty)
            {
                if (previous.Hash != requestHash) throw new RuleException(409,"Khóa yêu cầu đã được dùng cho nội dung đặt lịch khác.");
                if (!Secrets.Equal(previous.TokenHash,Secrets.Hash(replayToken!))) throw new RuleException(409,"Khóa cấu hình đã thay đổi. Hãy tra cứu lịch đã đặt, không tạo lại yêu cầu này.");
                await tx.CommitAsync();
                return (previous.Id,replayToken!);
            }
        }
        var slot = (await Read(c,"SELECT doctor_id,visit_date FROM slots WHERE id=$1",r=>(Doctor:r.GetInt32(0),Date:r.GetFieldValue<DateOnly>(1)),request.SlotId)).SingleOrDefault();
        if (slot.Doctor == 0) throw new RuleException(404,"Không tìm thấy khung giờ.");
        await LockQueue(c,slot.Doctor,slot.Date);
        if (await Execute(c,"UPDATE slots SET reserved=reserved+1 WHERE id=$1 AND reserved<capacity AND ends_at>now()",request.SlotId) != 1)
            throw new RuleException(409,"Khung giờ đã hết chỗ hoặc đã kết thúc.");
        var patient = Guid.NewGuid(); var id = Guid.NewGuid(); var token = replayToken ?? Secrets.NewToken();
        await Execute(c,"INSERT INTO patients VALUES($1,$2,$3)",patient,request.PatientName.Trim(),request.Phone.Trim());
        await Execute(c,"INSERT INTO appointments(id,patient_id,slot_id,doctor_id,visit_date,token_hash,status) VALUES($1,$2,$3,$4,$5,$6,'Pending')",id,patient,request.SlotId,slot.Doctor,slot.Date,Secrets.Hash(token));
        await Enqueue(c,"number.requested",id,slot.Doctor,slot.Date);
        // Availability changes immediately, even when the number worker is offline.
        await Enqueue(c,"queue.changed",id,slot.Doctor,slot.Date);
        if (keyHash is not null)
            await Execute(c,"INSERT INTO booking_requests(key_hash,request_hash,appointment_id) VALUES($1,$2,$3)",keyHash,requestHash,id);
        await tx.CommitAsync();
        return (id,token);
    }
    public async Task<bool> CanView(Guid id,string? token)
    {
        if (string.IsNullOrEmpty(token)) return false;
        await using var c = await Open();
        var hash = await Scalar<string>(c,"SELECT token_hash FROM appointments WHERE id=$1",id);
        return hash is not null && Secrets.Equal(hash,Secrets.Hash(token));
    }
    public async Task<Appointment> GetAppointment(Guid id)
    {
        await using var c = await Open();
        return (await Read(c,"SELECT a.id,a.doctor_id,a.slot_id,a.visit_date,p.name,a.status,a.number,s.starts_at,s.ends_at FROM appointments a JOIN patients p ON p.id=a.patient_id JOIN slots s ON s.id=a.slot_id WHERE a.id=$1",r=>new Appointment(r.GetGuid(0),r.GetInt32(1),r.GetGuid(2),r.GetFieldValue<DateOnly>(3),r.GetString(4),r.GetString(5),r.IsDBNull(6)?null:r.GetInt32(6),r.GetFieldValue<DateTimeOffset>(7),r.GetFieldValue<DateTimeOffset>(8)),id)).SingleOrDefault() ?? throw new RuleException(404,"Không tìm thấy lịch hẹn.");
    }
    public async Task Cancel(Guid id)
    {
        var appointment = await GetAppointment(id);
        await using var c = await Open(); await using var tx = await c.BeginTransactionAsync();
        await LockQueue(c,appointment.DoctorId,appointment.Date);
        var status = await Scalar<string>(c,"SELECT status FROM appointments WHERE id=$1 FOR UPDATE",id);
        if (status == "Cancelled") { await tx.CommitAsync(); return; }
        if (status is not ("Pending" or "Waiting")) throw new RuleException(409,"Chỉ hủy lịch đang chờ cấp số hoặc đang đợi khám.");
        await Execute(c,"UPDATE appointments SET status='Cancelled' WHERE id=$1",id);
        await Execute(c,"UPDATE slots SET reserved=reserved-1 WHERE id=$1",appointment.SlotId);
        await Enqueue(c,"queue.changed",id,appointment.DoctorId,appointment.Date);
        await tx.CommitAsync();
    }
    public async Task AssignNumber(Guid id)
    {
        var appointment = await GetAppointment(id);
        await using var c = await Open(); await using var tx = await c.BeginTransactionAsync();
        await LockQueue(c,appointment.DoctorId,appointment.Date);
        var status = await Scalar<string>(c,"SELECT status FROM appointments WHERE id=$1 FOR UPDATE",id);
        if (status != "Pending") { await tx.CommitAsync(); return; }
        if (appointment.Date < ClinicTime.Today)
        {
            await Execute(c,"UPDATE appointments SET status='Expired' WHERE id=$1",id);
            await Execute(c,"UPDATE slots SET reserved=reserved-1 WHERE id=$1",appointment.SlotId);
        }
        else
        {
            var number = await Scalar<int>(c,"UPDATE doctor_queues SET last_number=last_number+1 WHERE doctor_id=$1 AND visit_date=$2 RETURNING last_number",appointment.DoctorId,appointment.Date);
            await Execute(c,"UPDATE appointments SET number=$2,status='Waiting' WHERE id=$1",id,number);
        }
        await Enqueue(c,"queue.changed",id,appointment.DoctorId,appointment.Date);
        await tx.CommitAsync();
    }
    public async Task<QueueView> Queue(int doctor, DateOnly date)
    {
        await using var c = await Open();
        var tickets = await Read(c,"SELECT a.number,a.status,s.starts_at FROM appointments a JOIN slots s ON s.id=a.slot_id WHERE a.doctor_id=$1 AND a.visit_date=$2 AND a.status IN ('Waiting','Serving') ORDER BY s.starts_at,a.number",r=>new Ticket(r.GetInt32(0),r.GetString(1),r.GetFieldValue<DateTimeOffset>(2)),doctor,date);
        return new(doctor,date,tickets.SingleOrDefault(t=>t.Status=="Serving")?.Number,tickets.Where(t=>t.Status=="Waiting").ToList());
    }
    public async Task<QueueView> Advance(int doctor, DateOnly date, bool complete)
    {
        if (date != ClinicTime.Today) throw new RuleException(409,"Chỉ thao tác hàng đợi của ngày hôm nay theo giờ Việt Nam.");
        await using var c = await Open(); await using var tx = await c.BeginTransactionAsync();
        if (await Scalar<long>(c,"SELECT count(*) FROM doctors WHERE id=$1",doctor)==0) throw new RuleException(404,"Không tìm thấy bác sĩ.");
        await LockQueue(c,doctor,date);
        var current = await Scalar<Guid>(c,"SELECT id FROM appointments WHERE doctor_id=$1 AND visit_date=$2 AND status='Serving' FOR UPDATE",doctor,date);
        Guid changed;
        if (complete)
        {
            if (current==Guid.Empty) throw new RuleException(409,"Chưa có lượt đang khám.");
            changed=current;
            await Execute(c,"UPDATE appointments SET status='Completed' WHERE id=$1",current);
        }
        else
        {
            if (current!=Guid.Empty) throw new RuleException(409,"Hãy hoàn thành lượt đang khám trước khi gọi lượt mới.");
            changed = await Scalar<Guid>(c,"SELECT a.id FROM appointments a JOIN slots s ON s.id=a.slot_id WHERE a.doctor_id=$1 AND a.visit_date=$2 AND a.status='Waiting' AND s.starts_at<=now() ORDER BY s.starts_at,a.number LIMIT 1 FOR UPDATE OF a",doctor,date);
            if (changed==Guid.Empty) throw new RuleException(409,"Chưa có lượt đủ điều kiện trong khung giờ hiện tại.");
            await Execute(c,"UPDATE appointments SET status='Serving' WHERE id=$1",changed);
        }
        await Enqueue(c,"queue.changed",changed,doctor,date);
        await tx.CommitAsync();
        return await Queue(doctor,date);
    }
}
