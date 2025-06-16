using System.Reflection;
using Blazor.Redux;
using Blazor.Redux.Extensions;
using Blazor.Redux.Sample.Client.Pages;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);


var initialCounterSlice = new CounterSlice { Value = 10 };
builder.Services.AddBlazorRedux(new BlazorReduxOption()
{
    Slices = [initialCounterSlice]
});


await builder.Build().RunAsync();