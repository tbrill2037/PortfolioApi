using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Infra.Repository.Email;
using Portfolio.IntegrationTests.Fixtures;

namespace Portfolio.IntegrationTests.Email;

public sealed class EmailOutboxRepositoryTests(PostgreSqlFixtures fixture)
    : IClassFixture<PostgreSqlFixtures>
{
    [Fact]
    public async Task ClaimPendingAsync_ShouldClaimEligibleMessagesInCreatedOrder()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTime.UtcNow;

        await using var dbContext = fixture.CreateDbContext();

        await dbContext.EmailOutboxes.ExecuteDeleteAsync(cancellationToken);

        var oldestPending = CreateOutbox(now.AddMinutes(-5));
        var processed = CreateOutbox(
            now.AddMinutes(-4),
            processedUtc: now);
        var activelyLocked = CreateOutbox(
            now.AddMinutes(-3),
            processingLockUntilUtc: now.AddMinutes(5));
        var expiredLock = CreateOutbox(
            now.AddMinutes(-2),
            processingLockUntilUtc: now.AddMinutes(-5));
        var newestPending = CreateOutbox(now.AddMinutes(-1));
        var thirdPending = CreateOutbox(now);

        await dbContext.EmailOutboxes.AddRangeAsync(
            [
                oldestPending,
                processed,
                activelyLocked,
                expiredLock,
                newestPending,
                thirdPending
            ],
            cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);
        var lockUntilUtc = now.AddMinutes(10);

        // Act
        var claimed = await repository.ClaimPendingAsync(
            2,
            lockUntilUtc,
            cancellationToken);

        // Assert
        claimed.Should().HaveCount(2);

        claimed.Select(x => x.Id).Should().ContainInOrder(
            oldestPending.Id,
            expiredLock.Id);

        claimed.Should().OnlyContain(x =>
            x.ProcessingLockUntilUtc == lockUntilUtc &&
            x.ProcessingLockToken != null &&
            x.ProcessingUtc != null);
    }

    [Fact]
    public async Task MarkPublishedAsync_ShouldMarkMessageAsProcessedAndClearLock()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        entity.ProcessingLockToken = lockToken;
        entity.ProcessingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);
        entity.ProcessingUtc = DateTime.UtcNow;

        await dbContext.EmailOutboxes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);

        // Act
        var result = await repository.MarkPublishedAsync(
            entity.Id,
            lockToken,
            cancellationToken);

        // Assert
        result.Should().BeTrue();

        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.ProcessedUtc.Should().NotBeNull();
        persisted.ProcessingLockUntilUtc.Should().BeNull();
        persisted.ProcessingLockToken.Should().BeNull();
    }

    [Fact]
    public async Task MarkPublishedAsync_ShouldReturnFalseWhenLockTokenDoesNotMatch()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        entity.ProcessingLockToken = lockToken;
        entity.ProcessingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);

        await dbContext.EmailOutboxes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);

        // Act
        var result = await repository.MarkPublishedAsync(
            entity.Id,
            Guid.NewGuid(),
            cancellationToken);

        // Assert
        result.Should().BeFalse();

        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.ProcessedUtc.Should().BeNull();
        persisted.ProcessingLockToken.Should().Be(lockToken);
        persisted.ProcessingLockUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task ReleaseProcessingLockAsync_ShouldClearLockWhenTokenMatches()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        entity.ProcessingLockToken = lockToken;
        entity.ProcessingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);
        entity.ProcessingUtc = DateTime.UtcNow;

        await dbContext.EmailOutboxes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);

        // Act
        var result = await repository.ReleaseProcessingLockAsync(
            entity.Id,
            lockToken,
            cancellationToken);

        // Assert
        result.Should().BeTrue();

        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.ProcessingLockUntilUtc.Should().BeNull();
        persisted.ProcessingLockToken.Should().BeNull();
        persisted.ProcessedUtc.Should().BeNull();
    }

    [Fact]
    public async Task ReleaseProcessingLockAsync_ShouldReturnFalseWhenLockTokenDoesNotMatch()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        entity.ProcessingLockToken = lockToken;
        entity.ProcessingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);

        await dbContext.EmailOutboxes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);

        // Act
        var result = await repository.ReleaseProcessingLockAsync(
            entity.Id,
            Guid.NewGuid(),
            cancellationToken);

        // Assert
        result.Should().BeFalse();

        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.ProcessingLockToken.Should().Be(lockToken);
        persisted.ProcessingLockUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task IncrementRetryAsync_ShouldIncrementRetryCountAndReleaseLock()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();
        const string errorMessage = "Failed to publish message.";

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        entity.RetryCount = 2;
        entity.ProcessingLockToken = lockToken;
        entity.ProcessingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);

        await dbContext.EmailOutboxes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);

        // Act
        var result = await repository.IncrementRetryAsync(
            entity.Id,
            lockToken,
            errorMessage,
            cancellationToken);

        // Assert
        result.Should().BeTrue();

        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.RetryCount.Should().Be(3);
        persisted.ErrorMessage.Should().Be(errorMessage);
        persisted.ProcessingLockUntilUtc.Should().BeNull();
        persisted.ProcessingLockToken.Should().BeNull();
        persisted.ProcessedUtc.Should().BeNull();
    }

    [Fact]
    public async Task IncrementRetryAsync_ShouldReturnFalseWhenLockTokenDoesNotMatch()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        entity.RetryCount = 2;
        entity.ProcessingLockToken = lockToken;
        entity.ProcessingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);

        await dbContext.EmailOutboxes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);

        // Act
        var result = await repository.IncrementRetryAsync(
            entity.Id,
            Guid.NewGuid(),
            "Failed to publish message.",
            cancellationToken);

        // Assert
        result.Should().BeFalse();

        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.RetryCount.Should().Be(2);
        persisted.ErrorMessage.Should().BeNull();
        persisted.ProcessingLockToken.Should().Be(lockToken);
        persisted.ProcessingLockUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldMarkMessageAsProcessedAndReleaseLock()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();
        const string errorMessage = "Maximum retry attempts exceeded.";

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        entity.ProcessingLockToken = lockToken;
        entity.ProcessingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);

        await dbContext.EmailOutboxes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);

        // Act
        var result = await repository.MarkFailedAsync(
            entity.Id,
            lockToken,
            errorMessage,
            cancellationToken);

        // Assert
        result.Should().BeTrue();

        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.ErrorMessage.Should().Be(errorMessage);
        persisted.ProcessedUtc.Should().NotBeNull();
        persisted.ProcessingLockUntilUtc.Should().BeNull();
        persisted.ProcessingLockToken.Should().BeNull();
    }

    [Fact]
    public async Task MarkFailedAsync_ShouldReturnFalseWhenLockTokenDoesNotMatch()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var lockToken = Guid.NewGuid();

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        entity.ProcessingLockToken = lockToken;
        entity.ProcessingLockUntilUtc = DateTime.UtcNow.AddMinutes(5);

        await dbContext.EmailOutboxes.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var repository = new EmailOutboxRepository(dbContext);

        // Act
        var result = await repository.MarkFailedAsync(
            entity.Id,
            Guid.NewGuid(),
            "Maximum retry attempts exceeded.",
            cancellationToken);

        // Assert
        result.Should().BeFalse();

        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.ProcessedUtc.Should().BeNull();
        persisted.ErrorMessage.Should().BeNull();
        persisted.ProcessingLockToken.Should().Be(lockToken);
        persisted.ProcessingLockUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task AddAsync_ShouldAddOutboxMessage()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var dbContext = fixture.CreateDbContext();

        var entity = CreateOutbox(DateTime.UtcNow);
        var repository = new EmailOutboxRepository(dbContext);

        // Act
        await repository.AddAsync(entity, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Assert
        var persisted = await dbContext.EmailOutboxes
            .AsNoTracking()
            .SingleAsync(x => x.Id == entity.Id, cancellationToken);

        persisted.EmailMessageId.Should().Be(entity.EmailMessageId);
        persisted.PayloadJson.Should().Be(entity.PayloadJson);
        persisted.CreatedUtc.Should().BeCloseTo(entity.CreatedUtc, TimeSpan.FromMicroseconds(1));
    }

    [Fact]
    public async Task AddAsync_ShouldRejectDuplicateEmailMessageId()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;

        await using var dbContext = fixture.CreateDbContext();

        var emailMessageId = Guid.NewGuid();

        var first = CreateOutbox(DateTime.UtcNow);
        first.EmailMessageId = emailMessageId;

        var duplicate = CreateOutbox(DateTime.UtcNow);
        duplicate.EmailMessageId = emailMessageId;

        var repository = new EmailOutboxRepository(dbContext);

        await repository.AddAsync(first, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Act
        await repository.AddAsync(duplicate, cancellationToken);

        var act = async () => await dbContext.SaveChangesAsync(cancellationToken);

        // Assert
        await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task ClaimPendingAsync_ShouldPreventWorkersFromClaimingSameMessages()
    {
        // Arrange
        var cancellationToken = TestContext.Current.CancellationToken;
        var now = DateTime.UtcNow;

        await using var setupContext = fixture.CreateDbContext();

        await setupContext.EmailOutboxes.ExecuteDeleteAsync(cancellationToken);

        var messages = Enumerable.Range(0, 4)
            .Select(i => CreateOutbox(now.AddMinutes(i)))
            .ToList();

        await setupContext.EmailOutboxes.AddRangeAsync(messages, cancellationToken);
        await setupContext.SaveChangesAsync(cancellationToken);

        await using var firstContext = fixture.CreateDbContext();
        await using var secondContext = fixture.CreateDbContext();

        var firstRepository = new EmailOutboxRepository(firstContext);
        var secondRepository = new EmailOutboxRepository(secondContext);
        var lockUntilUtc = now.AddMinutes(10);

        // Act
        var firstClaimTask = firstRepository.ClaimPendingAsync(
            2, lockUntilUtc, cancellationToken);

        var secondClaimTask = secondRepository.ClaimPendingAsync(
            2, lockUntilUtc, cancellationToken);

        var results = await Task.WhenAll(firstClaimTask, secondClaimTask);

        var firstClaimed = results[0];
        var secondClaimed = results[1];

        // Assert
        firstClaimed.Should().HaveCount(2);
        secondClaimed.Should().HaveCount(2);

        firstClaimed.Select(x => x.Id)
            .Should()
            .NotIntersectWith(secondClaimed.Select(x => x.Id));

        firstClaimed.Concat(secondClaimed)
            .Select(x => x.Id)
            .Should()
            .BeEquivalentTo(messages.Select(x => x.Id));
    }

    private static EmailOutboxEntity CreateOutbox(
        DateTime createdUtc,
        DateTime? processedUtc = null,
        DateTime? processingLockUntilUtc = null)
    {
        return new EmailOutboxEntity
        {
            Id = Guid.NewGuid(),
            EmailMessageId = Guid.NewGuid(),
            PayloadJson = """{"recipient":"test@example.com"}""",
            CreatedUtc = createdUtc,
            ProcessedUtc = processedUtc,
            ProcessingLockUntilUtc = processingLockUntilUtc
        };
    }
}