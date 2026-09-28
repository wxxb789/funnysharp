using Microsoft.Extensions.Logging;

namespace FunnySharp.VerticalSlice.Tests;

/// <summary>Collects logged exceptions so a test can prove a failure was observed, not swallowed.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<Exception> exceptions = [];

    public IReadOnlyList<Exception> Exceptions
    {
        get
        {
            lock (exceptions)
            {
                return [.. exceptions];
            }
        }
    }

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(this);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(CapturingLoggerProvider owner) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception is null)
            {
                return;
            }

            lock (owner.exceptions)
            {
                owner.exceptions.Add(exception);
            }
        }
    }
}
