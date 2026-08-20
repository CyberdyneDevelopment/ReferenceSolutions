using Fdw.Collections.Attributes;
using Fdw.Services.Notifications.Abstractions;

namespace ReferenceNotifications.Teams;

/// <summary>
/// Microsoft Teams notification channel.
/// Sends notifications via Microsoft Teams webhook.
/// </summary>
// Why: data-bearing TypeOption; ctor only forwards literal/config data to the base class, no behavior
// Why no RestrictToCurrentCompilation: on a member that flag withholds it from consuming assemblies,
// and this channel does not live in the assembly that declares the collection. Left set, it never
// joins NotificationChannels and every lookup returns the NotFound stub.
[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[TypeOption(typeof(NotificationChannels), "Teams")]
public sealed class TeamsChannel : NotificationChannelBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TeamsChannel"/> class.
    /// </summary>
    public TeamsChannel()
        : base(
            id: 2,
            name: "Teams",
            description: "Microsoft Teams webhook notifications",
            supportsBatchSend: false,
            supportsRichContent: true,
            supportsAttachments: false,
            maxMessageLength: 28000) // Teams card limit
    {
    }
}
