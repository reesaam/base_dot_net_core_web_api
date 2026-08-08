namespace BaseWebApi.Infrastructure.Configuration;

public sealed class SqlServerOptions
{
    public const string SectionName = "SqlServer";
    public string ConnectionString { get; set; } = string.Empty;
}

public sealed class OracleOptions
{
    public const string SectionName = "Oracle";
    public string ConnectionString { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public string Configuration { get; set; } = "localhost:6379";
    public string InstanceName { get; set; } = "BaseWebApi:";
}

public sealed class MinioOptions
{
    public const string SectionName = "Minio";
    public string Endpoint { get; set; } = "localhost:9000";
    public string AccessKey { get; set; } = "minioadmin";
    public string SecretKey { get; set; } = "minioadmin";
    public string BucketName { get; set; } = "basewebapi";
    public bool UseSsl { get; set; }
}

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "basewebapi";
    public string DefaultTopic { get; set; } = "enterprise.events";
}

public sealed class ElasticsearchOptions
{
    public const string SectionName = "Elasticsearch";
    public string Url { get; set; } = "http://localhost:9200";
    public string DefaultIndex { get; set; } = "basewebapi";
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "BaseWebApi";
    public string Audience { get; set; } = "BaseWebApi.Clients";
    public string SigningKey { get; set; } = "CHANGE_ME_TO_A_LONG_RANDOM_SECRET_KEY_32+";
    public int ExpirationMinutes { get; set; } = 60;
}

public sealed class ApiOptions
{
    public const string SectionName = "Api";
    public string BaseUrl { get; set; } = "https://localhost:5001";
}
