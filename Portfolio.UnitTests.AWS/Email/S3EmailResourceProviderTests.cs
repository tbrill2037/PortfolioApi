using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.AWS.Configuration;
using Portfolio.Infra.AWS.Services.Email;

namespace Portfolio.UnitTests.AWS.Email;

public sealed class S3EmailResourceProviderTests
{
    [Fact]
    public async Task GetAsync_ShouldRetrieveResourceFromConfiguredBucket()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        const string bucketName = "test-email-resources";
        const string resourceKey = "attachments/report.pdf";

        byte[] expectedContent = [1, 2, 3, 4, 5];

        var s3 = new Mock<IAmazonS3>();

        var options = Options.Create(new S3Options
        {
            BucketName = bucketName
        });

        var resource = new EmailResource
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ResourceKey = resourceKey,
            Type = EmailResourceType.External
        };

        GetObjectRequest? capturedRequest = null;

        s3.Setup(x => x.GetObjectAsync(
                It.IsAny<GetObjectRequest>(),
                cancellationToken))
            .Callback<GetObjectRequest, CancellationToken>((request, _) =>
            {
                capturedRequest = request;
            })
            .ReturnsAsync(() =>
            {
                return new GetObjectResponse
                {
                    ResponseStream = new MemoryStream(expectedContent)
                };
            });

        var provider = new S3EmailResourceProvider(s3.Object, options);

        // Act
        var result = await provider.GetAsync(resource, cancellationToken);

        // Assert
        result.Should().Equal(expectedContent);

        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be(bucketName);
        capturedRequest.Key.Should().Be(resourceKey);

        provider.ResourceType.Should().Be(EmailResourceType.External);

        s3.Verify(
            x => x.GetObjectAsync(
                It.IsAny<GetObjectRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldPropagateS3Exception()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var s3 = new Mock<IAmazonS3>();

        var options = Options.Create(new S3Options
        {
            BucketName = "test-email-resources"
        });

        var resource = new EmailResource
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ResourceKey = "attachments/report.pdf",
            Type = EmailResourceType.External
        };

        var exception = new AmazonS3Exception("S3 request failed.");

        s3.Setup(x => x.GetObjectAsync(
                It.IsAny<GetObjectRequest>(),
                cancellationToken))
            .ThrowsAsync(exception);

        var provider = new S3EmailResourceProvider(s3.Object, options);

        // Act
        var act = () => provider.GetAsync(resource, cancellationToken);

        // Assert
        var thrownException = await act.Should()
            .ThrowAsync<AmazonS3Exception>();

        thrownException.Which.Should().BeSameAs(exception);

        s3.Verify(
            x => x.GetObjectAsync(
                It.IsAny<GetObjectRequest>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldForwardCancellationTokenToS3()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var s3 = new Mock<IAmazonS3>();

        var options = Options.Create(new S3Options
        {
            BucketName = "test-email-resources"
        });

        var resource = new EmailResource
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ResourceKey = "attachments/report.pdf",
            Type = EmailResourceType.External
        };

        byte[] expectedContent = [1, 2, 3];

        s3.Setup(x => x.GetObjectAsync(
                It.IsAny<GetObjectRequest>(),
                cancellationToken))
            .ReturnsAsync(new GetObjectResponse
            {
                ResponseStream = new MemoryStream(expectedContent)
            });

        var provider = new S3EmailResourceProvider(s3.Object, options);

        // Act
        var result = await provider.GetAsync(resource, cancellationToken);

        // Assert
        result.Should().Equal(expectedContent);

        s3.Verify(
            x => x.GetObjectAsync(
                It.IsAny<GetObjectRequest>(),
                It.Is<CancellationToken>(token => token == cancellationToken)),
            Times.Once);
    }
}