using System.Reflection;
using Blazor.Redux;
using Blazor.Redux.DevTools.Extensions;
using Blazor.Redux.Extensions;
using Blazor.Redux.Sample.Client.Pages;
using Blazor.Redux.Sample.Components;
using Blazor.Redux.Sample.Client;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<Blazor.Redux.Sample.Client.DispatchLogStore>();


var initialCounterSlice = new CounterSlice { Value = 10 };
builder.Services.AddBlazorRedux(new BlazorReduxOption()
{
    Slices = [initialCounterSlice, new EffectsSlice()],
    Assembly = typeof(Blazor.Redux.Sample.Client._Imports).Assembly
}.AddMiddleware<Blazor.Redux.Sample.Client.LoggingMiddleware>());

builder.Services.AddReduxDevTools();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blazor.Redux.Sample.Client._Imports).Assembly);

app.Run();
