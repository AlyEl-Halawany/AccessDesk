namespace AccessDesk.Domain.Enums;

public enum NotificationType
{
    RequestSubmitted,
    RequestApprovedByManager,
    RequestApprovedByOwner,
    RequestRejected,
    RequestCancelled,
    AccessExpired,
    AccessExpiringSoon,
    ReviewRequired,
    ReviewOverdue,
    SodConflictDetected
}
