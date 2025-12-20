using SoftielRemote.Backend.Api.Domain.Enums;

namespace SoftielRemote.Backend.Api.Domain.Entities;

public sealed class Device
{
    public Guid Id { get; init; }
    public Guid? OwnerUserId { get; init; }
    public string DeviceCode { get; init; } = default!;
    public string DeviceName { get; init; } = default!;
    public DeviceType DeviceType { get; init; }
    public bool IsOnline { get; init; }
    public DateTime LastSeenAt { get; init; }
    public DateTime CreatedAt { get; init; }
}

