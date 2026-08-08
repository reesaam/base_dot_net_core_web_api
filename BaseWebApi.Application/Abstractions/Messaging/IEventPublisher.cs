namespace BaseWebApi.Application.Abstractions.Messaging;

public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : class;
}
