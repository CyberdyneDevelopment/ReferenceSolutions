using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Sqlite.Commands;
using Fdw.Services.SecretManagers.Sqlite.Configuration;

namespace ReferenceSecretManagers.Sqlite.Tests;

internal sealed class CapturingLoggerFactory : ILoggerFactory
{
    private readonly List<string> _messages = [];

    public IReadOnlyList<string> Messages => _messages;

    public ILogger CreateLogger(string categoryName)
        => new CapturingLogger(_messages);

    public void AddProvider(ILoggerProvider provider) { }

    public void Dispose() { }

    private sealed class CapturingLogger : ILogger
    {
        private readonly List<string> _messages;

        public CapturingLogger(List<string> messages)
            => _messages = messages;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => _messages.Add(formatter(state, exception));
    }
}
