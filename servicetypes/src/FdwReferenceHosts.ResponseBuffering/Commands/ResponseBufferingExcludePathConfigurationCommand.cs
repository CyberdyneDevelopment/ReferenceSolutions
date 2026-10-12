using Fdw.Collections.Attributes;
using Fdw.Services.Configuration;
using FdwReferenceHosts.ResponseBuffering.Configuration;

namespace FdwReferenceHosts.ResponseBuffering.Commands;

/// <summary>Reads and writes the ResponseBufferingExcludePath child records.</summary>
/// <remarks>The implementation provider saves its children through their own command; a child with
/// none cannot be saved at all.</remarks>
[TypeOption(typeof(ConfigurationCommands), "ResponseBufferingExcludePath")]
public sealed class ResponseBufferingExcludePathConfigurationCommand : ConfigurationCommandBase<ResponseBufferingExcludePath>
{
    /// <summary>Initializes a new instance of the <see cref="ResponseBufferingExcludePathConfigurationCommand"/> class.</summary>
    public ResponseBufferingExcludePathConfigurationCommand()
        : base("ResponseBufferingExcludePath")
    {
    }
}
