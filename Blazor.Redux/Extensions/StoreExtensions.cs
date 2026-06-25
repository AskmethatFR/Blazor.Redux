using Blazor.Redux.Core;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Extensions;

/// <summary>
/// Extension methods for convenient slice access from the store.
/// </summary>
public static class StoreExtensions
{
    /// <summary>
    /// Gets a slice from the store or returns a default instance if not found.
    /// </summary>
    /// <typeparam name="T">Slice type with parameterless constructor.</typeparam>
    /// <param name="store">The store.</param>
    /// <returns>Existing slice or a new default instance.</returns>
    public static T GetSliceOrDefault<T>(this Store store) where T : class, ISlice, new()
    {
        return store.GetSlice<T>() ?? new T();
    }

    /// <summary>
    /// Gets a slice from the store or returns the specified default value if not found.
    /// </summary>
    /// <typeparam name="T">Slice type.</typeparam>
    /// <param name="store">The store.</param>
    /// <param name="defaultValue">Default value to return if slice is not registered.</param>
    /// <returns>Existing slice or the provided default value.</returns>
    public static T GetSliceOrDefault<T>(this Store store, T defaultValue) where T : class, ISlice
    {
        return store.GetSlice<T>() ?? defaultValue;
    }

    /// <summary>
    /// Gets a property from a slice, returning a default value if the slice is not registered.
    /// </summary>
    /// <typeparam name="TSlice">Slice type.</typeparam>
    /// <typeparam name="TResult">Property type.</typeparam>
    /// <param name="store">The store.</param>
    /// <param name="selector">Property selector.</param>
    /// <param name="defaultValue">Default value if slice is not found.</param>
    /// <returns>Selected property value or default.</returns>
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
