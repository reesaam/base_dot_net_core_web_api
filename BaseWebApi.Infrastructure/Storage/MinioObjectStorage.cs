using BaseWebApi.Application.Abstractions.Storage;
using BaseWebApi.Infrastructure.Configuration;
using BaseWebApi.Shared.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;

namespace BaseWebApi.Infrastructure.Storage;

public sealed class MinioObjectStorage(
    IMinioClient client,
    IOptions<MinioOptions> options,
    ILogger<MinioObjectStorage> logger)
    : IObjectStorage
{
    private readonly MinioOptions _options = options.Value;

    public async Task<string> UploadAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketAsync(cancellationToken);

        var putArgs = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey)
            .WithStreamData(content)
            .WithObjectSize(content.CanSeek ? content.Length : -1)
            .WithContentType(contentType);

        await client.PutObjectAsync(putArgs, cancellationToken);
        logger.LogInformation("Uploaded object {ObjectKey} to bucket {Bucket}", objectKey, _options.BucketName);
        return objectKey;
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var memory = new MemoryStream();
        var args = new GetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey)
            .WithCallbackStream(stream => stream.CopyTo(memory));

        await client.GetObjectAsync(args, cancellationToken);
        memory.Position = 0;
        return memory;
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        var args = new RemoveObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey);

        await client.RemoveObjectAsync(args, cancellationToken);
    }

    public async Task<string> GetPresignedUrlAsync(
        string objectKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        var args = new PresignedGetObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectKey)
            .WithExpiry((int)expiry.TotalSeconds);

        return await client.PresignedGetObjectAsync(args);
    }

    private async Task EnsureBucketAsync(CancellationToken cancellationToken)
    {
        var existsArgs = new BucketExistsArgs().WithBucket(_options.BucketName);
        var exists = await client.BucketExistsAsync(existsArgs, cancellationToken);
        if (exists)
        {
            return;
        }

        try
        {
            var makeArgs = new MakeBucketArgs().WithBucket(_options.BucketName);
            await client.MakeBucketAsync(makeArgs, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InfrastructureException(
                $"Unable to ensure MinIO bucket '{_options.BucketName}'.",
                ex,
                "minio.bucket");
        }
    }
}
