using FluentValidation;

namespace ReferenceMultitenancy.Sql;

/// <summary>
/// Validator for <see cref="SqlTenantConfiguration"/>.
/// </summary>
public sealed class SqlTenantConfigurationValidator : AbstractValidator<SqlTenantConfiguration>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SqlTenantConfigurationValidator"/> class.
    /// </summary>
    public SqlTenantConfigurationValidator()
    {
        RuleFor(x => x.DataStoreName)
            .NotEmpty()
            .WithMessage("DataStoreName is required for tenant data access.");

        RuleFor(x => x.PathName)
            .NotEmpty()
            .WithMessage("PathName (schema) is required for tenant data access.");

        RuleFor(x => x.TenantsTableName)
            .NotEmpty()
            .WithMessage("TenantsTableName is required.");

        RuleFor(x => x.TenantFeaturesTableName)
            .NotEmpty()
            .WithMessage("TenantFeaturesTableName is required.");

        RuleFor(x => x.TenantRolesTableName)
            .NotEmpty()
            .WithMessage("TenantRolesTableName is required.");

        RuleFor(x => x.UserTenantsTableName)
            .NotEmpty()
            .WithMessage("UserTenantsTableName is required.");
    }
}
