using Fdw.Services.SecretManagers;
using FdwReferenceSecretManagers.EnvironmentVariable.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FdwReferenceConnections.FileSystem.Tests.Integration;

/// <summary>
/// End-to-end proof that a database-less host can read FDW configuration from a local JSON folder
/// through the UNCHANGED <c>ConfigurationGateway</c>: <c>AddConfigurationGateway&lt;FileSystemConnectionFactory&gt;</c>
/// resolves the ConfigurationDb connection declared in <c>configurationSchema.json</c> via the
/// FileSystem connection type (container <c>Format="Json"</c>), and
/// <see cref="SecretManagerConfigurationProvider"/> reads the domain row (<c>sec/SecretManager.json</c>),
/// takes the implementation it names, and reads that implementation's row
/// (<c>sec/EnvironmentVariableSecretManagerConfiguration.json</c>, joined on the FK declared in the schema's container
/// Keys) exactly as it would against MsSql.
/// </summary>
/// <remarks>
/// This passes on the fully-resolved system-level design (both former blockers fixed in the shared
/// <c>Fdw.Data.DataNodes</c> builder layer, not the connection package):
/// <para>
/// FILE ADDRESSING — a dedicated <c>FileSystemDataStoreBuilder</c> (sibling of <c>GenericDataStoreBuilder</c>)
/// builds each container's physical <c>Path</c> as the FULL relative file path
/// <c>{DataPath folder}/{container name}{format.CanonicalFileExtension}</c> (e.g. <c>sec/SecretManager.json</c>),
/// mirroring how <c>MsSqlDataStoreBuilder</c> composes a two-part <c>{schema}.{object}</c> address. The
/// extension is read from the new <see cref="Fdw.Data.Abstractions.IFormatType.CanonicalFileExtension"/>
/// (it is NOT derivable from the format name), so the domain and implementation tables under one DataPath resolve to
/// DISTINCT files. A format with no canonical extension (not file-addressable) fails loud in the builder's
/// <c>ValidateConfiguration</c>. The <c>FileSystemCommandTranslator</c> uses <c>container.Path.PathValue</c>
/// directly — the round-trip tests (which set <c>Path.PathValue</c> to a full file path) stay green.
/// </para>
/// <para>
/// SIBLING NAVIGATION — <c>DataStoreBuilderBase.Build</c> now constructs the final <c>DataPath</c> first and
/// wires every container under THAT same populated path via <c>DataPath.SetContainers</c>, so
/// <c>container.Parent.Container("SecretManager")</c> resolves. Previously each container was parented to a
/// throwaway empty placeholder path (a different object from the populated one), so the typed-body in-memory
/// JOIN missed. The fix is in the shared base and benefits every transport; <c>DataContainer</c> stays fully
/// immutable (its <c>Parent</c> is set once, to the correct path).
/// </para>
/// </remarks>
[Collection("FileSystemRecordFormats")]
public sealed class FileSystemConnectionGatewayTests : IClassFixture<FileSystemConnectionGatewayFixture>
{
    private readonly FileSystemConnectionGatewayFixture _fixture;

    public FileSystemConnectionGatewayTests(FileSystemConnectionGatewayFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByNameReadsTheImplementationItsDomainRowNamesThroughTheFileSystemConnection()
    {
        var secretManagerProvider = _fixture.Provider.GetRequiredService<SecretManagerConfigurationProvider>();

        var result = await secretManagerProvider.Get("EnvSecrets", TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value.ShouldNotBeNull();
        result.Value!.Name.ShouldBe("EnvSecrets");
        result.Value.ShouldBeOfType<EnvironmentVariableConfiguration>();
        ((EnvironmentVariableConfiguration)result.Value!).Prefix.ShouldBe("FDW_SECRET_");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetByIdReadsTheSameImplementationAsGetByName()
    {
        var secretManagerProvider = _fixture.Provider.GetRequiredService<SecretManagerConfigurationProvider>();
        var id = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var result = await secretManagerProvider.Get(id, TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue(result.CurrentMessage?.ToString());
        result.Value.ShouldNotBeNull();
        result.Value!.Id.ShouldBe(id);
        result.Value!.Name.ShouldBe("EnvSecrets");
        result.Value.ShouldBeOfType<EnvironmentVariableConfiguration>();
        ((EnvironmentVariableConfiguration)result.Value!).Prefix.ShouldBe("FDW_SECRET_");
    }

    // End-to-end write through the REAL provider: Save writes the domain row and the implementation row together
    // against the FileSystem-backed gateway, and Get re-reads it -- proving the write path is real. A DISTINCT
    // name keeps this isolated from the seeded EnvSecrets the read tests assert.
    [Fact]
    [Trait("Category", "Integration")]
    public async Task SaveThroughProviderPersistsDomainAndImplementationAndIsVisibleOnReread()
    {
        var provider = _fixture.Provider.GetRequiredService<SecretManagerConfigurationProvider>();

        var saveResult = await provider.Save(
            new EnvironmentVariableConfiguration { Prefix = "TEMP_" },
            "SecretManager", "EnvironmentVariable", "TempSecrets",
            TestContext.Current.CancellationToken);
        saveResult.IsSuccess.ShouldBeTrue(saveResult.CurrentMessage?.ToString());

        var readResult = await provider.Get("TempSecrets", TestContext.Current.CancellationToken);

        readResult.IsSuccess.ShouldBeTrue(readResult.CurrentMessage?.ToString());
        readResult.Value.ShouldNotBeNull();
        readResult.Value!.Name.ShouldBe("TempSecrets");
        readResult.Value.ShouldBeOfType<EnvironmentVariableConfiguration>();
        ((EnvironmentVariableConfiguration)readResult.Value!).Prefix.ShouldBe("TEMP_");
    }
}
