using System.Threading.Tasks;
using EventFlow.Shared.Messaging;

namespace EventFlow.EventApi.Services;

public interface IRabbitMqPublisher
{
    Task PublishAsync<T>(string routingKey, T message) where T : class, IEventMessage;
}
