using System.Reflection;
using Blazor.Redux.Interfaces;

namespace Blazor.Redux.Extensions;

public record BlazorReduxOption
{
    public ISlice[] Slices { get; set; } = [];
    public Assembly? Assembly { get; set; } = null;
}