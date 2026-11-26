namespace AccessDesk.Domain.Enums;

/// <summary>
/// The level of access a user is requesting for an application.
/// These map to what the application owner defines as valid access levels.
/// </summary>
public enum AccessLevel
{
    Read,
    Write,
    Admin
}
