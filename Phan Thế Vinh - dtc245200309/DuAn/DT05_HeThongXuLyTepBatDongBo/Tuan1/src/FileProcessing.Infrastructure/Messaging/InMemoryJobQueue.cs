using System.Threading.Channels;
using FileProcessing.Core.Contracts;

namespace FileProcessing.Infrastructure.Messaging;

public interface IJobQueueChannel
{
    ValueTask EnqueueAsync(ProcessImageJobMessage message, CancellationToken cancellationToken = default);
    IAsyncEnumerable<ProcessImageJobMessage> ReadAllAsync(CancellationToken cancellationToken = default);
    ValueTask<ProcessImageJobMessage> DequeueAsync(CancellationToken cancellationToken = default);
}

public class InMemoryJobQueue : IJobQueueChannel
{
    private readonly Channel<ProcessImageJobMessage> _channel;

    public InMemoryJobQueue()
    {
        var options = new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };
        _channel = Channel.CreateBounded<ProcessImageJobMessage>(options);
    }

    public ValueTask EnqueueAsync(ProcessImageJobMessage message, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(message, cancellationToken);
    }

    public IAsyncEnumerable<ProcessImageJobMessage> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public ValueTask<ProcessImageJobMessage> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAsync(cancellationToken);
    }
}
