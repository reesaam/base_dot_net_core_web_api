namespace BaseWebApi.Shared.Constants;

/// <summary>
/// Cross-cutting constants reusable across any enterprise module.
/// TODO: Extend with module-specific constant classes as features grow.
/// </summary>
public static class AppConstants
{
    public const string ApplicationName = "BaseWebApi";
    public const string DefaultCorsPolicy = "DefaultCors";
    public const string ApiVersionHeader = "X-Api-Version";

    public static class Auth
    {
        public const string BearerScheme = "Bearer";
        public const string PermissionClaim = "permission";
        public const string TenantClaim = "tenant_id";
    }

    public static class Pagination
    {
        public const int DefaultPage = 1;
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 200;
    }

    public static class Cache
    {
        public const string ItemPrefix = "item:";
        public static TimeSpan DefaultTtl { get; } = TimeSpan.FromMinutes(15);
    }

    public static class Messaging
    {
        public const string ItemCreatedTopic = "items.created";
        public const string ItemUpdatedTopic = "items.updated";
        public const string ItemDeletedTopic = "items.deleted";
    }

    public static class Search
    {
        public const string ItemsIndex = "items";
    }

    public static class Storage
    {
        public const long MaxUploadBytes = 20 * 1024 * 1024; // 20 MB
        public static readonly string[] AllowedContentTypes =
        [
            "image/jpeg",
            "image/png",
            "image/webp",
            "application/pdf",
            "text/plain",
            "application/json"
        ];
    }
}
