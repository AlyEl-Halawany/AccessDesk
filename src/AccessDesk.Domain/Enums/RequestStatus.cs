namespace AccessDesk.Domain.Enums;

/// <summary>
/// The lifecycle states of an access request.
/// The state machine is:
///   PendingManager → PendingOwner → Approved
///   PendingManager → Rejected (by manager)
///   PendingOwner   → Rejected (by owner)
///   Any active state → Cancelled (by employee)
///   Approved → Expired (by background service when duration elapses)
///   Any state → FlaggedForReview (when SoD conflict is detected)
/// </summary>
public enum RequestStatus
{
    PendingManager,    // Waiting for the employee's manager to approve
    PendingOwner,      // Manager approved; waiting for the application owner
    Approved,          // Both parties approved; access is active
    Rejected,          // Rejected by manager or owner
    Cancelled,         // Cancelled by the requesting employee
    Expired,           // Was approved but the duration has elapsed
    FlaggedForReview   // SoD conflict detected; SecurityAdmin must decide
}
