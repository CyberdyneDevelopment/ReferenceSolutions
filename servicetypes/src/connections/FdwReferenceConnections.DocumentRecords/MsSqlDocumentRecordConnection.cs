using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Documents.Records.Abstractions;
using Fdw.Documents.Records.Abstractions.Capabilities;
using Fdw.Documents.Records.Abstractions.Commands;
using Fdw.Documents.Records.Abstractions.Definitions;
using Fdw.Documents.Records.Abstractions.Links;
using Fdw.Documents.Records.Abstractions.Results;
using Fdw.Results;
using Fdw.Services.Connections.Abstractions;
using FdwReferenceConnections.DocumentRecords.Logging;
using FdwReferenceConnections.DocumentRecords.Rows;
using FdwReferenceConnections.DocumentRecords.Sql;
using FdwReferenceConnections.MsSql;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConnections.DocumentRecords;

/// <summary>
/// SQL Server keeps an organization's document records (FDW-3731): this wraps a <see cref="MsSqlConnection"/> and carries out FDW's document
/// record commands (FDW-3673) as SQL over the doc tables of the database it reaches (databases/DocumentRecordsDb). Each write and the raise
/// of the organization's revision run in one transaction, so it declares <c>AtomicRevisionWrites</c>.
/// </summary>
/// <remarks>
/// Every statement runs on one of the SQL Server connection's pooled connections, with its command timeout and the caller's session
/// context, the way the script extension's do. Values are SQL parameters and never part of the text; the tables are named here and only
/// here. It holds no state of its own: FDW makes one for each lookup.
/// </remarks>
public sealed class MsSqlDocumentRecordConnection : IDocumentRecordConnection
{
    private const string NoRevision = "none";

    private static readonly IReadOnlyList<string> TargetKinds = [LinkTargetKinds.Document, LinkTargetKinds.Record];

    private readonly MsSqlConnection _connection;
    private readonly IGenericConnection _generic;
    private readonly DocumentRecordSql _sql;

    /// <summary>Initializes a new instance of the <see cref="MsSqlDocumentRecordConnection"/> class.</summary>
    /// <param name="connection">The SQL Server connection the records are kept through.</param>
    public MsSqlDocumentRecordConnection(MsSqlConnection connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _generic = connection;
        _sql = new DocumentRecordSql(connection);
        RecordCapabilities = [DocumentRecordCapabilities.ByName("AtomicRevisionWrites")];
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<IDocumentRecordCapability> RecordCapabilities { get; }

    /// <inheritdoc/>
    public string Id => _generic.Id;

    /// <inheritdoc/>
    public string ServiceType => _generic.ServiceType;

    /// <inheritdoc/>
    public bool IsAvailable => _generic.IsAvailable;

    /// <inheritdoc/>
    public string Name => _generic.Name;

    /// <inheritdoc/>
    public async Task<IGenericResult<T>> Execute<T>(IDocumentRecordCommand<T> command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (command.FirstProblem() is { } problem)
            return NotAnswered<T>(command.CommandType, problem);

        object? answer = await DocumentReads(command, cancellationToken).ConfigureAwait(false);
        answer ??= await TreeReads(command, cancellationToken).ConfigureAwait(false);
        answer ??= await Links(command, cancellationToken).ConfigureAwait(false);
        answer ??= await RecordWrites(command, cancellationToken).ConfigureAwait(false);
        answer ??= await HistoryWrites(command, cancellationToken).ConfigureAwait(false);
        answer ??= await Definitions(command, cancellationToken).ConfigureAwait(false);
        return answer switch
        {
            IGenericResult<T> typed => typed,
            null => NotAnswered<T>(command.CommandType, "no statement of this connection answers it"),
            _ => NotAnswered<T>(command.CommandType, "its answer is not the result type the command asks for"),
        };
    }

    /// <inheritdoc/>
    public Task<IGenericResult<T>> Execute<T>(IGenericCommand command, CancellationToken cancellationToken = default) =>
        _generic.Execute<T>(command, cancellationToken);

    /// <inheritdoc/>
    public Task<IGenericResult> Execute(IGenericCommand command, CancellationToken cancellationToken = default) =>
        _generic.Execute(command, cancellationToken);

    /// <inheritdoc/>
    public Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default) =>
        _generic.TestConnection(cancellationToken);

    /// <inheritdoc/>
    public void Dispose() => _connection.Dispose();

    private async Task<object?> DocumentReads(IGenericCommand command, CancellationToken cancellationToken) => command switch
    {
        GetDocumentRecordCommand get => await _sql.One<IDocumentRecord>(
            get.OrganizationId, "Document", get.Id, ReadStatements.Document(get.OrganizationId, get.Id), DocumentRecordRow.From, cancellationToken).ConfigureAwait(false),
        ListDocumentRecordsCommand list => await _sql.Rows<IDocumentRecord>(
            list.OrganizationId, ReadStatements.Documents(list.OrganizationId, list.DocumentType), DocumentRecordRow.From, cancellationToken).ConfigureAwait(false),
        ListStoredMediaCommand media => await _sql.Rows<IStoredMedia>(
            media.OrganizationId, ReadStatements.Media(media.OrganizationId, media.DocumentId), StoredMediaRow.From, cancellationToken).ConfigureAwait(false),
        ListAttributeValuesCommand values => await _sql.Rows<IAttributeValue>(
            values.OrganizationId, ReadStatements.AttributeValues(values.OrganizationId, values.OwnerId), AttributeValueRow.From, cancellationToken).ConfigureAwait(false),
        ListStateChangesCommand changes => await _sql.Rows<IStateChange>(
            changes.OrganizationId, ReadStatements.StateChanges(changes.OrganizationId, changes.DocumentId), StateChangeRow.From, cancellationToken).ConfigureAwait(false),
        _ => null,
    };

    private async Task<object?> TreeReads(IGenericCommand command, CancellationToken cancellationToken) => command switch
    {
        GetRecordNodeCommand node => await _sql.One<IRecordNode>(
            node.OrganizationId, "Record", node.Id, ReadStatements.Node(node.OrganizationId, node.Id), RecordNodeRow.From, cancellationToken).ConfigureAwait(false),
        ListRecordChildrenCommand { ParentId: { } parentId } children => await _sql.Rows<IRecordNode>(
            children.OrganizationId, ReadStatements.Children(children.OrganizationId, parentId), RecordNodeRow.From, cancellationToken).ConfigureAwait(false),
        ListRecordChildrenCommand top => await _sql.Rows<IRecordNode>(
            top.OrganizationId, ReadStatements.TopNodes(top.OrganizationId), RecordNodeRow.From, cancellationToken).ConfigureAwait(false),
        ListRecordRequirementsCommand requirements => await _sql.Rows<IRecordRequirement>(
            requirements.OrganizationId, ReadStatements.Requirements(requirements.OrganizationId, requirements.RecordId), RecordRequirementRow.From, cancellationToken).ConfigureAwait(false),
        ListReminderEntriesCommand reminders => await _sql.Rows<IReminderEntry>(
            reminders.OrganizationId, ReadStatements.Reminders(reminders.OrganizationId, reminders.RequirementId), ReminderEntryRow.From, cancellationToken).ConfigureAwait(false),
        _ => null,
    };

    private async Task<object?> Links(IGenericCommand command, CancellationToken cancellationToken) => command switch
    {
        GetOrganizationRevisionCommand revision => await _sql.One<IOrganizationRevision>(
            revision.OrganizationId, "OrganizationRevision", revision.OrganizationId, ReadStatements.Revision(revision.OrganizationId), OrganizationRevisionRow.From, cancellationToken).ConfigureAwait(false),
        ListLinksFromCommand from => await _sql.Rows<IDocumentLink>(
            from.OrganizationId, ReadStatements.LinksFrom(from.OrganizationId, from.DocumentId), DocumentLinkRow.From, cancellationToken).ConfigureAwait(false),
        ListLinksToCommand to => await _sql.Rows<IDocumentLink>(
            to.OrganizationId, ReadStatements.LinksTo(to.OrganizationId, to.TargetKind, to.TargetId), DocumentLinkRow.From, cancellationToken).ConfigureAwait(false),
        ListLinkTargetKindsCommand => GenericResult<IReadOnlyList<string>>.Success(TargetKinds),
        LinkTargetExistsCommand exists => await TargetExists(exists, cancellationToken).ConfigureAwait(false),
        _ => null,
    };

    private async Task<object?> RecordWrites(IGenericCommand command, CancellationToken cancellationToken) => command switch
    {
        SaveDocumentRecordCommand { Document: { } document } save => await _sql.Revised(
            save.CommandType, save.OrganizationId, save.ExpectedRevision, WriteStatements.SaveDocument(save.OrganizationId, document), cancellationToken).ConfigureAwait(false),
        AddStoredMediaCommand { Media: { } media } add => await _sql.Revised(
            add.CommandType, add.OrganizationId, add.ExpectedRevision, WriteStatements.AddMedia(add.OrganizationId, media), cancellationToken).ConfigureAwait(false),
        SaveRecordNodeCommand { Node: { } node } saveNode => await _sql.Revised(
            saveNode.CommandType, saveNode.OrganizationId, saveNode.ExpectedRevision, WriteStatements.SaveNode(saveNode.OrganizationId, node), cancellationToken).ConfigureAwait(false),
        SetAttributeValueCommand { Value: { } value } set => await _sql.Revised(
            set.CommandType, set.OrganizationId, set.ExpectedRevision, WriteStatements.SetAttribute(set.OrganizationId, value), cancellationToken).ConfigureAwait(false),
        _ => null,
    };

    private async Task<object?> HistoryWrites(IGenericCommand command, CancellationToken cancellationToken) => command switch
    {
        EnterStateCommand { Change: { } change } enter => await _sql.Revised(
            enter.CommandType, enter.OrganizationId, enter.ExpectedRevision, WriteStatements.EnterState(enter.OrganizationId, change), cancellationToken).ConfigureAwait(false),
        SaveRecordRequirementCommand { Requirement: { } requirement } saveRequirement => await _sql.Revised(
            saveRequirement.CommandType, saveRequirement.OrganizationId, saveRequirement.ExpectedRevision, WriteStatements.SaveRequirement(saveRequirement.OrganizationId, requirement), cancellationToken).ConfigureAwait(false),
        AddDocumentLinkCommand { Link: { } link } addLink => await _sql.Revised(
            addLink.CommandType, addLink.OrganizationId, addLink.ExpectedRevision, WriteStatements.AddLink(addLink.OrganizationId, link), cancellationToken).ConfigureAwait(false),
        RemoveDocumentLinkCommand remove => await _sql.Revised(
            remove.CommandType, remove.OrganizationId, remove.ExpectedRevision, WriteStatements.RemoveLink(remove.OrganizationId, remove.Id), cancellationToken).ConfigureAwait(false),
        RecordReminderCommand { Entry: { } entry } record => await Reminder(record, entry, cancellationToken).ConfigureAwait(false),
        StartOrganizationRevisionCommand start => await Start(start, cancellationToken).ConfigureAwait(false),
        _ => null,
    };

    private async Task<object?> Definitions(IGenericCommand command, CancellationToken cancellationToken) => command switch
    {
        ReadDocumentTypeDefinitionsCommand types => await _sql.Rows<IDocumentTypeDefinition>(
            types.TenantId, DefinitionStatements.DocumentTypes(types.TenantId, types.OrganizationId), DocumentTypeDefinitionRow.From, cancellationToken).ConfigureAwait(false),
        ReadAttributeDefinitionsCommand attributes => await _sql.Rows<IAttributeDefinition>(
            attributes.TenantId, DefinitionStatements.Attributes(attributes.TenantId, attributes.OrganizationId), AttributeDefinitionRow.From, cancellationToken).ConfigureAwait(false),
        ReadRecordKindDefinitionsCommand kinds => await _sql.Rows<IRecordKindDefinition>(
            kinds.TenantId, DefinitionStatements.RecordKinds(kinds.TenantId, kinds.OrganizationId), RecordKindDefinitionRow.From, cancellationToken).ConfigureAwait(false),
        ReadWorkflowStageDefinitionsCommand stages => await _sql.Rows<IWorkflowStageDefinition>(
            stages.TenantId, DefinitionStatements.WorkflowStages(stages.TenantId, stages.OrganizationId), WorkflowStageDefinitionRow.From, cancellationToken).ConfigureAwait(false),
        ReadRequirementRuleDefinitionsCommand rules => await _sql.Rows<IRequirementRuleDefinition>(
            rules.TenantId, DefinitionStatements.RequirementRules(rules.TenantId, rules.OrganizationId), RequirementRuleDefinitionRow.From, cancellationToken).ConfigureAwait(false),
        ReadLinkKindDefinitionsCommand linkKinds => await _sql.Rows<ILinkKindDefinition>(
            linkKinds.TenantId, DefinitionStatements.LinkKinds(linkKinds.TenantId, linkKinds.OrganizationId), LinkKindDefinitionRow.From, cancellationToken).ConfigureAwait(false),
        _ => null,
    };

    private async Task<IGenericResult<bool>> TargetExists(LinkTargetExistsCommand command, CancellationToken cancellationToken)
    {
        if (ReadStatements.TargetFound(command.OrganizationId, command.TargetKind, command.TargetId) is not { } statement)
        {
            var kinds = string.Join(", ", TargetKinds);
            DocumentRecordSqlLog.TargetKindUnknown(_connection.ScriptLogger, command.TargetKind, kinds);
            return GenericResult<bool>.Failure(
                DocumentRecordResultCodes.ByName("DocumentLinkTargetKindUnknown"),
                ResultDetails.Create("TargetKind", command.TargetKind, "Kinds", kinds));
        }

        return await _sql.One<bool>(
            command.OrganizationId, command.TargetKind, command.TargetId, statement, Found, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IGenericResult<bool>> Reminder(RecordReminderCommand command, IReminderEntry entry, CancellationToken cancellationToken)
    {
        var recorded = await _sql.Unrevised(
            command.CommandType, command.OrganizationId, WriteStatements.RecordReminder(command.OrganizationId, entry), cancellationToken).ConfigureAwait(false);
        return recorded.IsFailure
            ? recorded.ToNewResult<bool>()
            : GenericResult<bool>.Success(recorded.Value == 1);
    }

    // Starts the organization's records at revision 1, or refuses when they were already started.
    private async Task<IGenericResult<IOrganizationRevision>> Start(StartOrganizationRevisionCommand command, CancellationToken cancellationToken)
    {
        var organizationId = command.OrganizationId;
        var started = await _sql.Unrevised(
            command.CommandType, organizationId, WriteStatements.StartRevision(organizationId), cancellationToken).ConfigureAwait(false);
        if (started.IsFailure)
            return started.ToNewResult<IOrganizationRevision>();

        if (started.Value == 1)
            return GenericResult<IOrganizationRevision>.Success(new OrganizationRevisionRow(organizationId, 1));

        var stored = await _sql.One<IOrganizationRevision>(
            organizationId, "OrganizationRevision", organizationId, ReadStatements.Revision(organizationId), OrganizationRevisionRow.From, cancellationToken).ConfigureAwait(false);
        if (!stored.IsSuccess || stored.Value is not { } current)
            return stored;

        return _sql.Conflict<IOrganizationRevision>(organizationId, current.Revision.ToString(CultureInfo.InvariantCulture), NoRevision);
    }

    private IGenericResult<T> NotAnswered<T>(string command, string reason)
    {
        DocumentRecordSqlLog.CommandNotAnswered(_connection.ScriptLogger, command, reason);
        return GenericResult<T>.Failure(
            DocumentRecordResultCodes.ByName("DocumentRecordCommandRefused"),
            ResultDetails.Create("Command", command, "Reason", reason));
    }

    private static IGenericResult<bool> Found(SqlDataReader reader) => GenericResult<bool>.Success(reader.GetBoolean(0));
}
