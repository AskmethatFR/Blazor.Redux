using System.Reactive.Linq;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Extensions;

public static class ObservableStoreExtensions
{
    public static IObservable<TResult> SelectSlice<TSlice, TResult>(
        this IObservableStore store,
        Func<TSlice, TResult> selector)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Select(selector)
            .DistinctUntilChanged();
    }

    public static IObservable<TSlice> WhereSlice<TSlice>(
        this IObservableStore store,
        Func<TSlice, bool> predicate)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Where(predicate);
    }

    public static IObservable<TProperty> ObserveProperty<TSlice, TProperty>(
        this IObservableStore store,
        Func<TSlice, TProperty> propertySelector)
        where TSlice : class, ISlice
    {
        return store.ObserveSlice<TSlice>()
            .Select(propertySelector)
            .DistinctUntilChanged();
    }

    // Observer une propriété avec comparateur personnalisé
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

    // Observer plusieurs propriétés combinées
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

    // Observer une propriété avec condition
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