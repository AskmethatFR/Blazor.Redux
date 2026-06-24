using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Blazor.Redux.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Blazor.Redux.Core;

public sealed class EffectsPipeline : IDisposable
{
    private readonly IActionStream _actionStream;
    private readonly IRootStateStore _rootStateStore;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IReadOnlyList<IEffect> _effects;
    private readonly IScheduler? _effectsScheduler;
    private readonly EffectsCancellationStrategy _cancellationStrategy;
    private readonly CompositeDisposable _subscriptions = new();
    private readonly ILogger<EffectsPipeline>? _logger;
    private int _started;

    public EffectsPipeline(
        IActionStream actionStream,
        IRootStateStore rootStateStore,
        IServiceScopeFactory scopeFactory,
        IEnumerable<IEffect> effects,
        IScheduler? effectsScheduler,
        EffectsCancellationStrategy cancellationStrategy,
        ILogger<EffectsPipeline>? logger = null)
    {
        _actionStream = actionStream ?? throw new ArgumentNullException(nameof(actionStream));
        _rootStateStore = rootStateStore ?? throw new ArgumentNullException(nameof(rootStateStore));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _effects = (effects ?? throw new ArgumentNullException(nameof(effects))).ToList();
        _effectsScheduler = effectsScheduler;
        _cancellationStrategy = cancellationStrategy;
        _logger = logger;
    }

    public void EnsureStarted()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
        {
            return;
        }

        if (_effects.Count == 0)
        {
            return;
        }

        var actions = _actionStream.Actions;
        var state = _rootStateStore.ObserveState();

        foreach (var effect in _effects)
        {
            var observable = Observable.Defer(() => BuildEffectObservable(effect, actions, state))
                .Do(_ => { }, ex => _logger?.LogError(ex, "Effect {EffectType} failed", effect.GetType().Name))
                .Retry(3);
            if (_effectsScheduler != null)
            {
                observable = observable.ObserveOn(_effectsScheduler);
            }

            var subscription = observable.Subscribe(DispatchEffectAction);

            _subscriptions.Add(subscription);
        }
    }

    public void Dispose()
    {
        _subscriptions.Dispose();
    }

    private void DispatchEffectAction(IEffectAction effectAction)
    {
        if (effectAction is null)
        {
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
            effectAction.Dispatch(dispatcher);
        }
        catch (Exception ex)
        {
            // Avoid terminating the effect subscription on dispatch errors.
            _logger?.LogError(ex, "EffectsPipeline failed to dispatch effect action {ActionType}", effectAction.GetType().Name);
        }
    }

    private IObservable<IEffectAction> BuildEffectObservable(
        IEffect effect,
        IObservable<IAction> actions,
        IObservable<RootStateSnapshot> state)
    {
        if (_cancellationStrategy == EffectsCancellationStrategy.RxSwitch &&
            effect is ICancelableEffect cancelableEffect)
        {
            return cancelableEffect.HandleWithCancellation(actions, state).Switch();
        }

        return effect.Handle(actions, state);
    }
}
