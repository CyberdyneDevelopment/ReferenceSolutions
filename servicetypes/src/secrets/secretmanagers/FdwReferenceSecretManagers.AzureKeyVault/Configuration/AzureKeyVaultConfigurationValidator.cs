using System;
using System.Collections.Generic;
using Fdw.Conventions;
using System.Linq;
using FluentValidation;

namespace FdwReferenceSecretManagers.AzureKeyVault.Configuration;

/// <summary>
/// Validator for Azure Key Vault configuration.
/// </summary>
/// <remarks>
/// Validates Azure Key Vault configuration settings to ensure proper authentication
/// and connection parameters are provided based on the selected authentication method.
/// </remarks>
public sealed class AzureKeyVaultConfigurationValidator : AbstractValidator<AzureKeyVaultConfiguration>
{
    private static readonly string[] ValidAuthenticationMethods =
    [
        "ManagedIdentity",
        "ServicePrincipal",
        "Certificate",
        "DeviceCode"
    ];

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultConfigurationValidator"/> class.
    /// </summary>
    public AzureKeyVaultConfigurationValidator()
    {
        ConfigureBasicValidation();
        ConfigureAuthenticationValidation();
        ConfigureOptionalSettingsValidation();
    }

    private void ConfigureBasicValidation()
    {
        RuleFor(x => x.VaultUri)
            .NotEmpty()
            .WithMessage("VaultUri is required.")
            .Must(BeValidVaultUri)
            .WithMessage("VaultUri must be a valid Azure Key Vault URI (https://<vault-name>.vault.azure.net/).");

        RuleFor(x => x.AuthenticationMethod)
            .NotEmpty()
            .WithMessage("AuthenticationMethod is required.")
            .Must(BeValidAuthenticationMethod)
            .WithMessage($"AuthenticationMethod must be one of: {string.Join(", ", ValidAuthenticationMethods)}");
    }

    private void ConfigureAuthenticationValidation()
    {
        ConfigureServicePrincipalValidation();
        ConfigureCertificateValidation();
        ConfigureManagedIdentityValidation();
    }

    private void ConfigureServicePrincipalValidation()
    {
        When(x => string.Equals(x.AuthenticationMethod, "ServicePrincipal", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required for ServicePrincipal authentication.")
                .Must(BeValidGuid)
                .WithMessage("TenantId must be a valid GUID.");

            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage("ClientId is required for ServicePrincipal authentication.")
                .Must(BeValidGuid)
                .WithMessage("ClientId must be a valid GUID.");

            RuleFor(x => x.ClientSecret)
                .NotEmpty()
                .WithMessage("ClientSecret is required for ServicePrincipal authentication.")
                .MinimumLength(8)
                .WithMessage("ClientSecret must be at least 8 characters long.");
        });
    }

    private void ConfigureCertificateValidation()
    {
        When(x => string.Equals(x.AuthenticationMethod, "Certificate", StringComparison.OrdinalIgnoreCase), () =>
        {
            RuleFor(x => x.TenantId)
                .NotEmpty()
                .WithMessage("TenantId is required for Certificate authentication.")
                .Must(BeValidGuid)
                .WithMessage("TenantId must be a valid GUID.");

            RuleFor(x => x.ClientId)
                .NotEmpty()
                .WithMessage("ClientId is required for Certificate authentication.")
                .Must(BeValidGuid)
                .WithMessage("ClientId must be a valid GUID.");

            RuleFor(x => x.CertificatePath)
                .NotEmpty()
                .WithMessage("CertificatePath is required for Certificate authentication.")
                .Must(BeValidCertificatePath)
                .WithMessage("CertificatePath must have a .pfx or .p12 extension.");
        });
    }

    private void ConfigureManagedIdentityValidation()
    {
        When(x => string.Equals(x.AuthenticationMethod, "ManagedIdentity", StringComparison.OrdinalIgnoreCase) &&
                 !string.IsNullOrWhiteSpace(x.ManagedIdentityId), () =>
        {
            RuleFor(x => x.ManagedIdentityId)
                .Must(BeValidManagedIdentityId)
                .WithMessage("ManagedIdentityId must be a valid GUID, object ID, or resource ID.");
        });
    }

    private void ConfigureOptionalSettingsValidation()
    {
        ConfigureTimeoutValidation();
        ConfigurePageSizeValidation();
        ConfigureRetryPolicyValidation();
        ConfigureHeadersValidation();
    }

    private void ConfigureTimeoutValidation()
    {
        When(x => x.Timeout.HasValue, () =>
        {
            RuleFor(x => x.Timeout)
                .Must(timeout => timeout!.Value.TotalSeconds >= 1 && timeout.Value.TotalMinutes <= 10)
                .WithMessage("Timeout must be between 1 second and 10 minutes.");
        });
    }

    private void ConfigurePageSizeValidation()
    {
        When(x => x.MaxSecretsPerPage.HasValue, () =>
        {
            RuleFor(x => x.MaxSecretsPerPage)
                .InclusiveBetween(1, 25)
                .WithMessage("MaxSecretsPerPage must be between 1 and 25.");
        });
    }

    private void ConfigureRetryPolicyValidation()
    {
        When(x => x.RetryPolicy?.Count > 0, () =>
        {
            RuleFor(x => x.RetryPolicy)
                .Must(HaveValidRetryPolicySettings)
                .WithMessage("RetryPolicy contains invalid settings.");
        });
    }

    private void ConfigureHeadersValidation()
    {
        When(x => x.AdditionalHeaders?.Count > 0, () =>
        {
            RuleFor(x => x.AdditionalHeaders)
                .Must(HaveValidHeaders)
                .WithMessage("AdditionalHeaders contains invalid header names or values.");
        });
    }

    private static bool BeValidVaultUri(string? vaultUri)
    {
        if (string.IsNullOrWhiteSpace(vaultUri))
            return false;

        if (!Uri.TryCreate(vaultUri, UriKind.Absolute, out var uri))
            return false;

        return string.Equals(uri.Scheme, "https", StringComparison.OrdinalIgnoreCase) &&
               uri.Host.EndsWith(".vault.azure.net", StringComparison.OrdinalIgnoreCase);
    }

    private static bool BeValidAuthenticationMethod(string? authenticationMethod)
    {
        if (string.IsNullOrWhiteSpace(authenticationMethod))
            return false;

        return ValidAuthenticationMethods.Any(method =>
            string.Equals(method, authenticationMethod, StringComparison.OrdinalIgnoreCase));
    }

    private static bool BeValidGuid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out _);
    }

    private static bool BeValidCertificatePath(string? certificatePath)
    {
        if (string.IsNullOrWhiteSpace(certificatePath))
            return false;

        var extension = System.IO.Path.GetExtension(certificatePath).ToLowerInvariant();
        return string.Equals(extension, ".pfx", StringComparison.Ordinal) ||
               string.Equals(extension, ".p12", StringComparison.Ordinal);
    }

    private static bool BeValidManagedIdentityId(string? managedIdentityId)
    {
        if (string.IsNullOrWhiteSpace(managedIdentityId))
            return false;

        // Check if it's a GUID (client ID or object ID)
        if (Guid.TryParse(managedIdentityId, out _))
            return true;

        // Check if it's a resource ID (starts with /subscriptions/)
        return managedIdentityId.StartsWith("/subscriptions/", StringComparison.OrdinalIgnoreCase);
    }

    [ConventionOverride(MaxCyclomaticComplexity = 20)]
    private static bool HaveValidRetryPolicySettings(IReadOnlyDictionary<string, object>? retryPolicy)
    {
        if (retryPolicy == null || retryPolicy.Count == 0)
            return true;

        // Validate MaxRetries
        if (retryPolicy.TryGetValue("MaxRetries", out var maxRetriesObj) &&
            maxRetriesObj is int maxRetries &&
            (maxRetries < 0 || maxRetries > 10))
        {
            return false;
        }

        // Validate InitialDelay
        if (retryPolicy.TryGetValue("InitialDelay", out var initialDelayObj) &&
            initialDelayObj is TimeSpan initialDelay &&
            (initialDelay.TotalMilliseconds < 100 || initialDelay.TotalSeconds > 60))
        {
            return false;
        }

        // Validate MaxDelay
        if (retryPolicy.TryGetValue("MaxDelay", out var maxDelayObj) &&
            maxDelayObj is TimeSpan maxDelay &&
            (maxDelay.TotalSeconds < 1 || maxDelay.TotalMinutes > 5))
        {
            return false;
        }

        // Validate BackoffMultiplier
        if (retryPolicy.TryGetValue("BackoffMultiplier", out var multiplierObj) &&
            multiplierObj is double multiplier &&
            (multiplier < 1.0 || multiplier > 10.0))
        {
            return false;
        }

        return true;
    }

    private static bool HaveValidHeaders(IReadOnlyDictionary<string, string>? headers)
    {
        if (headers == null || headers.Count == 0)
            return true;

        foreach (var header in headers)
        {
            // Check for valid header name (no spaces, special characters)
            if (string.IsNullOrWhiteSpace(header.Key) ||
                header.Key.Any(c => char.IsWhiteSpace(c) || c == ':'))
            {
                return false;
            }

            // Check for valid header value (not null)
            if (header.Value == null)
            {
                return false;
            }
        }

        return true;
    }
}