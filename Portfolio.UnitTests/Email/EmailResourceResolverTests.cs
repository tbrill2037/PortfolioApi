
using FluentAssertions;
using Moq;
using Portfolio.Application.Common.Interfaces.Email;
using Portfolio.Application.Common.Models.Email;
using Portfolio.Domain.Enums.Email;
using Portfolio.Infra.Services.Email;
using Xunit;

namespace Portfolio.UnitTests.Email;

public sealed class EmailResourceResolverTests
{
    [Fact]
    public async Task ResolveAsync_ShouldUseProviderMatchingResourceType()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        byte[] expectedContent = [1, 2, 3];

        var resource = new EmailResource
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ResourceKey = "attachments/report.pdf",
            Type = EmailResourceType.External
        };

        var provider = new Mock<IEmailResourceProvider>();

        provider.SetupGet(x => x.ResourceType)
            .Returns(EmailResourceType.External);

        provider.Setup(x => x.GetAsync(resource, cancellationToken))
            .ReturnsAsync(expectedContent);

        var resolver = new EmailResourceResolver([provider.Object]);

        // Act
        var result = await resolver.ResolveAsync(resource, cancellationToken);

        // Assert
        result.Should().Equal(expectedContent);

        provider.Verify(
            x => x.GetAsync(resource, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ShouldThrowWhenNoProviderSupportsResourceType()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        var resource = new EmailResource
        {
            FileName = "report.pdf",
            ContentType = "application/pdf",
            ResourceKey = "attachments/report.pdf",
            Type = EmailResourceType.External
        };

        var provider = new Mock<IEmailResourceProvider>();

        provider.SetupGet(x => x.ResourceType)
            .Returns(EmailResourceType.Embedded);

        var resolver = new EmailResourceResolver([provider.Object]);

        // Act
        var act = () => resolver.ResolveAsync(resource, cancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<NotSupportedException>();

        provider.Verify(
            x => x.GetAsync(
                It.IsAny<EmailResource>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void Constructor_ShouldThrowWhenDuplicateProviderTypesAreRegistered()
    {
        // Arrange
        var firstProvider = new Mock<IEmailResourceProvider>();
        firstProvider.SetupGet(x => x.ResourceType)
            .Returns(EmailResourceType.External);

        var secondProvider = new Mock<IEmailResourceProvider>();
        secondProvider.SetupGet(x => x.ResourceType)
            .Returns(EmailResourceType.External);

        // Act
        var act = () => new EmailResourceResolver(
            [firstProvider.Object, secondProvider.Object]);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
