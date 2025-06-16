using System.Reflection;
using Blazor.Redux;
using Blazor.Redux.Extensions;
using Blazor.Redux.Sample.Client.Pages;
using Blazor.Redux.Sample.Components;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

var initialCounterSlice = new CounterSlice { Value = 10 };
builder.Services.AddRedux(initialCounterSlice);

var testAssembly = Assembly.GetExecutingAssembly();
builder.Services.AddReducers();

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