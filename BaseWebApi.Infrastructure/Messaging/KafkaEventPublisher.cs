using System.Text.Json;
using Confluent.Kafka;
using BaseWebApi.Application.Abstractions.Messaging;
using BaseWebApi.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BaseWebApi.Infrastructure.Messaging;

public sealed class KafkaEventPublisher : IEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public KafkaEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        var kafka = options.Value;
        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            ClientId = kafka.ClientId,
            Acks = Acks.All
        }).Build();
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        where T : class
    {
        var payload = JsonSerializer.Serialize(message, JsonOptions);
        var key = Guid.NewGuid().ToString("N");

        try
        {
            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string> { Key = key, Value = payload },
                cancellationToken);

            _logger.LogInformation(
                "Published Kafka message to {Topic} at offset {Offset}",
                result.Topic,
                result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            // Do not crash request pipeline when Kafka is temporarily down.
            _logger.LogWarning(ex, "Kafka publish to {Topic} failed; continuing without broker ack", topic);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Kafka publish to {Topic} failed unexpectedly; continuing", topic);
        }
    }

    public void Dispose() => _producer.Dispose();
}
