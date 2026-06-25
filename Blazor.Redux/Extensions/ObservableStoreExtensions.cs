using System.Reactive.Linq;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Extensions;

/// <summary>
/// Extension methods for reactive store queries — selecting, filtering,
/// and observing properties from the store's observable slices.
/// </summary>
public static class ObservableStoreExtensions
{
    /// <summary>
    /// Selects a projected value from a slice observable, distinct until changed.
    /// </summary>
    /// <typeparam name="TSlice">Slice type.</typeparam>
    /// <typeparam name="TResult">Projection result type.</typeparam>
    /// <param name="store">The observable store.</param>
    /// <param name="selector">Projection function.</param>
    /// <returns>Observable of distinct projected values.</returns>
    public static IObservable<TResult> SelectSlice<TSlice, TResult>(
        this IObservableStore store,
        Func<TSlice, TResult> selector)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Select(selector)
            .DistinctUntilChanged();
    }

    /// <summary>
    /// Filters a slice observable by a predicate.
    /// </summary>
    /// <typeparam name="TSlice">Slice type.</typeparam>
    /// <param name="store">The observable store.</param>
    /// <param name="predicate">Filter predicate.</param>
    /// <returns>Observable of slices matching the predicate.</returns>
    public static IObservable<TSlice> WhereSlice<TSlice>(
        this IObservableStore store,
        Func<TSlice, bool> predicate)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Where(predicate);
    }

    /// <summary>
    /// Observes a single property from a slice, distinct until changed.
    /// </summary>
    /// <typeparam name="TSlice">Slice type.</typeparam>
    /// <typeparam name="TProperty">Property type.</typeparam>
    /// <param name="store">The observable store.</param>
    /// <param name="propertySelector">Property selector function.</param>
    /// <returns>Observable of distinct property values.</returns>
    public static IObservable<TProperty> ObserveProperty<TSlice, TProperty>(
        this IObservableStore store,
        Func<TSlice, TProperty> propertySelector)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Select(propertySelector)
            .DistinctUntilChanged();
    }

    /// <summary>
    /// Observes a single property from a slice with a custom equality comparer.
    /// </summary>
    /// <typeparam name="TSlice">Slice type.</typeparam>
    /// <typeparam name="TProperty">Property type.</typeparam>
    /// <param name="store">The observable store.</param>
    /// <param name="propertySelector">Property selector function.</param>
    /// <param name="comparer">Custom equality comparer for distinct detection.</param>
    /// <returns>Observable of property values, distinct per comparer.</returns>
    public static IObservable<TProperty> ObserveProperty<TSlice, TProperty>(
        this IObservableStore store,
        Func<TSlice, TProperty> propertySelector,
        IEqualityComparer<TProperty> comparer)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Select(propertySelector)
            .DistinctUntilChanged(comparer);
    }

    /// <summary>
    /// Observes multiple properties combined via a combiner function, distinct until changed.
    /// </summary>
    public static IObservable<TResult> ObserveProperties<TSlice, T1, T2, TResult>(
        this IObservableStore store,
        Func<TSlice, T1> selector1,
        Func<TSlice, T2> selector2,
        Func<T1, T2, TResult> combiner)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Select(slice => combiner(selector1(slice), selector2(slice)))
            .DistinctUntilChanged();
    }

    /// <summary>
    /// Observes a property from a slice only when a condition is met, distinct until changed.
    /// </summary>
    public static IObservable<TProperty> ObservePropertyWhen<TSlice, TProperty>(
        this IObservableStore store,
        Func<TSlice, TProperty> propertySelector,
        Func<TSlice, bool> condition)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Where(condition)
            .Select(propertySelector)
            .DistinctUntilChanged();
    }
}
