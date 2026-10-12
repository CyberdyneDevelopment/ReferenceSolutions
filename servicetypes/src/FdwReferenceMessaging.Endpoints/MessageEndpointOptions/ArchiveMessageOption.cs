using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The ArchiveMessage endpoint.</summary>
[WebMcpTool("archive_message", "Archive message.")]
[TypeOption(typeof(MessageEndpoints), "ArchiveMessage")]
public class ArchiveMessageOption : MessageEndpointBase<ArchiveMessageEndpoint>
{
}
