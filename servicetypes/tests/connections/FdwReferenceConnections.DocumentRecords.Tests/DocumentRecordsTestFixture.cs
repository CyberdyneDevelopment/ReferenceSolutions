using System;
using Fdw.Services.Connections.MsSql;
using Fdw.Services.Connections.MsSql.Authentication;
using Fdw.Services.SecretManagers.Abstractions;
using FdwReferenceConnections.MsSql;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.DocumentRecords.Tests;

/// <summary>Loads the SQL Server authentication types the factory reads, and builds a SQL Server connection that is never opened.</summary>
public sealed class DocumentRecordsTestFixture
{
    public DocumentRecordsTestFixture()
    {
        _ = MsSqlAuthenticationTypes.All();
    }

    /// <summary>A SQL Server connection to a server nothing reaches: the tests only build statements and refusals, never run SQL.</summary>
    internal static MsSqlConnection SqlServer()
    {
        var factory = new MsSqlConnectionFactory(
            NullLogger<MsSqlConnectionFactory>.Instance,
            NullLogger<MsSqlConnection>.Instance,
            new Moq.Mock<ISecretManagerProvider>().Object,
            authenticationContextAccessor: null,
            settings: null) { DefaultMaxParametersBeforeTempTable = 50 };
        var created = factory.Create(
            new MsSqlConnectionConfiguration
            {
                Port = 1433,
                Encrypt = true,
                EnableConnectionPooling = true,
                MaxPoolSize = 100,
                DefaultSchema = "dbo",
                Name = "RecordsDb",
                MaxSerialInsert = 1000,
                CommandTimeoutSeconds = 30,
                ConnectionTimeoutSeconds = 15,
                Server = "localhost",
                Database = "DocumentRecordsDb",
                AuthenticationType = "WindowsAuth",
            },
            resolvedSecret: null);
        created.IsSuccess.ShouldBeTrue(created.CurrentMessage);
        return created.Value.ShouldBeOfType<MsSqlConnection>();
    }

    /// <summary>A UUIDv7, as the application sets every public id.</summary>
    internal static Guid NewId() => Guid.CreateVersion7();
}

[CollectionDefinition(nameof(DocumentRecordsTestCollection))]
public sealed class DocumentRecordsTestCollection : ICollectionFixture<DocumentRecordsTestFixture>
{
}
