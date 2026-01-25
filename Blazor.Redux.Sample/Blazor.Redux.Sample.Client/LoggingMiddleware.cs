using Blazor.Redux.Interfaces;
using Microsoft.Extensions.Logging;

namespace Blazor.Redux.Sample.Client;

public class LoggingMiddleware : IDispatchMiddleware
{
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly DispatchLogStore _dispatchLog;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger, DispatchLogStore dispatchLog)
    {
        _logger = logger;
        _dispatchLog = dispatchLog;
    }

    public async Task InvokeAsync<TSlice, TAction>(TAction action, Func<Task> next)
        where TSlice : class, ISlice
        where TAction : class, IAction
    {
        _logger.LogInformation("Dispatching {Action} for {Slice}", typeof(TAction).Name, typeof(TSlice).Name);
        _dispatchLog.Add($"Dispatching {typeof(TAction).Name} for {typeof(TSlice).Name}");
        await next();
        _logger.LogInformation("Dispatched {Action} for {Slice}", typeof(TAction).Name, typeof(TSlice).Name);
        _dispatchLog.Add($"Dispatched {typeof(TAction).Name} for {typeof(TSlice).Name}");
    }
}
