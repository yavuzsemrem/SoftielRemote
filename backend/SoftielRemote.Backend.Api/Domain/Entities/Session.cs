using SoftielRemote.Backend.Api.Domain.Enums;

namespace SoftielRemote.Backend.Api.Domain.Entities;

public sealed class Session
{
    public Guid Id { get; init; }
    public Guid HostDeviceId { get; init; }
    public Guid ClientDeviceId { get; init; }
    public SessionStatus Status { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? ConnectedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public string? EndReason { get; init; }
}

