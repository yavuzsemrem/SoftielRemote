namespace SoftielRemote.Backend.Api.Domain.Enums;

public enum SessionStatus
{
    Created = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Connected = 4,
    Ended = 5,
    Failed = 6
}

