namespace GeoLibrary.Server.Abstractions;

/// <summary>
/// Nomi delle policy di autorizzazione, per non ripetere stringhe magiche
/// tra la registrazione (ProgramExtensions.AddAuth) e gli attributi [Authorize].
/// </summary>
public static class AuthPolicies
{
    /// <summary>
    /// Richiede il ruolo di realm Keycloak "admin".
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>Nome del ruolo di realm come appare in realm_access.roles.</summary>
    public const string AdminRole = "admin";
}
