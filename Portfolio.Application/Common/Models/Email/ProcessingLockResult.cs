using Portfolio.Domain.Enums.Email;

namespace Portfolio.Application.Common.Models.Email;

/// <summary>
/// Represents the result of attempting to acquire a processing lock.
/// </summary>
public sealed record ProcessingLockResult
(
    bool Acquired,
    Guid? Token,
    DateTime? ExistingLockUntilUtc,
    ProcessingLockFailureReason FailureReason
);