using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Extensions;

public static class StoreExtensions
{
    // Extension pour récupérer un slice de façon plus élégante
    public static T GetSliceOrDefault<T>(this Store store) where T : class, ISlice, new()
    {
        return store.GetSlice<T>() ?? new T();
    }

    // Extension avec valeur par défaut personnalisée
    public static T GetSliceOrDefault<T>(this Store store, T defaultValue) where T : class, ISlice
    {
        return store.GetSlice<T>() ?? defaultValue;
    }

    // Extension pour récupérer une propriété directement
    public static TResult GetSliceProperty<TSlice, TResult>(
        this Store store,
        Func<TSlice, TResult> selector,
        TResult defaultValue = default!)
        where TSlice : class, ISlice
    {
        var slice = store.GetSlice<TSlice>();
        return slice != null ? selector(slice) : defaultValue;
    }
}