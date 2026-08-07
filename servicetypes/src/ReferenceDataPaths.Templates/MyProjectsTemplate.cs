using Fdw.Collections.Attributes;
using Fdw.Data.DataPaths;

namespace ReferenceDataPaths.Templates;

/// <summary>
/// Worked example of a tenant-scoped <see cref="DataPathTemplateBase"/>: a per-project file area
/// addressed as <c>{tenantId}/{projectName}/{filename}</c> over a FileSystem data store.
/// </summary>
/// <remarks>
/// Why this is a package rather than an application type: it joins FDW's <c>DataPathTemplates</c>
/// collection through a module initializer, which runs once per assembly at load. Declared inside a
/// host it would only register when that host loads, and could not be exercised without starting it.
/// </remarks>
[TypeOption(typeof(DataPathTemplates), "MyProjects")]
public sealed class MyProjectsTemplate : DataPathTemplateBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MyProjectsTemplate"/> class.
    /// </summary>
    public MyProjectsTemplate() : base(
        id: 1,
        name: "MyProjects",
        template: "{tenantId}/{projectName}/{filename}",
        dataStoreServiceType: "FileSystem",
        defaultPolicyName: "TenantScoped",
        requiredVariables: ["projectName", "filename"])
    {
        // Why: tenantId is injected server-side by IDataStore.Resolve from the caller's
        // IRequestContext.TenantId — callers must not supply it directly in Variables.
    }
}
