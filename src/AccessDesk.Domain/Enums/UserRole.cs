namespace AccessDesk.Domain.Enums;

/// <summary>
/// The roles a user can have in the system. A user can hold multiple roles
/// (e.g., someone can be both a Manager and an Employee).
/// </summary>
public enum UserRole
{
    Employee,
    Manager,
    AppOwner,
    SecurityAdmin
}
