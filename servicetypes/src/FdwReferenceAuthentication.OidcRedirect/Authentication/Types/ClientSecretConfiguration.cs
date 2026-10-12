using System.Collections.Generic;
using Fdw.Collections.Attributes;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using FdwReferenceAuthentication.OidcRedirect.Authentication.Results;
using Fdw.Services.Connections.Abstractions.Authentication;

namespace FdwReferenceAuthentication.OidcRedirect.Authentication.Types;

/// <summary>
/// Confidential client: the code exchange also sends a client secret.
/// Property keys: SecretManagerName, SecretKeyName.
/// </summary>
[TypeOption(typeof(OidcClientAuthenticationTypes), "ClientSecret")]
public sealed class ClientSecretConfiguration : OidcClientAuthenticationConfiguration
{
    private static readonly string[] Expected = ["SecretManagerName", "SecretKeyName"];
    private static readonly string[] Required = ["SecretManagerName", "SecretKeyName"];
    private static readonly string[] Secret = ["SecretKeyName"];

    /// <summary>Initializes a new instance of the <see cref="ClientSecretConfiguration"/> class.</summary>
    public ClientSecretConfiguration()
        : base("ClientSecret",
               "Client secret",
               "A client secret read from a named secret manager and sent with the code exchange",
               Expected, Required, Secret, requiresSecret: true)
    {
    }

    /// <inheritdoc/>
    public override IGenericResult Validate(IReadOnlyDictionary<string, string?> values)
    {
        var errors = new List<string>();
        values.TryGetValue("SecretManagerName", out var secretManagerName);
        values.TryGetValue("SecretKeyName", out var secretKeyName);
        if (string.IsNullOrWhiteSpace(secretManagerName))
            errors.Add("SecretManagerName is required for ClientSecret");
        if (string.IsNullOrWhiteSpace(secretKeyName))
            errors.Add("SecretKeyName is required for ClientSecret");

        return errors.Count == 0
            ? GenericResult.Success()
            : GenericResult.Failure(
                OidcClientAuthenticationResultCodes.ByName("ClientAuthenticationInvalid"),
                ResultDetails.Create("ValidationErrors", string.Join("; ", errors)));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// SecretManagerName and SecretKeyName are this type's own properties, so naming the store and the
    /// key belongs here. Naming the manager rather than accepting one means the step reads the store
    /// the authority declared and has no way to prefer a different one.
    /// </remarks>
    public override IGenericResult<SecretRequirement> RequiredSecret(IReadOnlyDictionary<string, string?> values)
    {
        var validation = Validate(values);
        if (!validation.IsSuccess)
            return validation.ToNewResult<SecretRequirement>();

        return GenericResult<SecretRequirement>.Success(
            new SecretRequirement(values["SecretManagerName"]!, values["SecretKeyName"]!));
    }

    /// <inheritdoc />
    public override IReadOnlyList<ConnectionCredentialField> Credentials { get; } = [new("SecretKeyName", "Client secret", secret: true)];

    /// <inheritdoc/>
    /// <remarks>The client authenticates with its own secret.</remarks>
    public override bool UsesHostIdentity(IReadOnlyDictionary<string, string?> values) => false;
}
