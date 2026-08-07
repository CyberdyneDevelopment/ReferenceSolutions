// Why: these tests moved out of Fdw.Services.Connections.<Kind>, so types that used to resolve
// through the enclosing namespace — the connection aggregation, the components it composes, and
// Fdw.Services.Connections members like ConnectionTypes and DataStoreConfiguration — now need
// explicit imports. Declaring them globally keeps the churn out of the individual test files.
global using System.Diagnostics.CodeAnalysis;
global using Fdw.Services.Connections;
global using Fdw.Services.Connections.Sqlite;
global using ReferenceConnections.Sqlite;
