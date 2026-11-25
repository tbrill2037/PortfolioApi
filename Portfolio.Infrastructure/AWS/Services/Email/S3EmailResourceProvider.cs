using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.AWS.Configuration;

namespace Portfolio.Infra.AWS.Services.Email;

/// <summary>
/// Provides access to email resources stored in Amazon S3.
/// </summary>
public sealed class S3EmailResourceProvider : IEmailResourceProvider
{
    private readonly IAmazonS3 _s3;
    private readonly S3Options _options;

    public S3EmailResourceProvider(IAmazonS3 s3, IOptions<S3Options> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public EmailResourceType ResourceType => EmailResourceType.External;

    /// <summary>
    /// Loads an email resource from Amazon S3.
    /// </summary>
    public async Task<byte[]> GetAsync(EmailResource resource, CancellationToken cancellationToken = default)
    {
        using var response = await _s3.GetObjectAsync(
            new GetObjectRequest
            {
                BucketName = _options.BucketName,
                Key = resource.ResourceKey
            },
            cancellationToken);

        using var memory = new MemoryStream();

        await response.ResponseStream.CopyToAsync(memory, cancellationToken);

        return memory.ToArray();
    }
}