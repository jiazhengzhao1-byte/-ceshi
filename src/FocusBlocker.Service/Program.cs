using FocusBlocker.Service;
using FocusBlocker.Service.Engine;
using FocusBlocker.Service.IPC;
using FocusBlocker.Service.Policy;
using FocusBlocker.Service.Storage;
using FocusBlocker.Shared;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<ITimeProvider, SystemTimeProvider>();
builder.Services.AddSingleton<ConfigStore>();
builder.Services.AddSingleton<StateStore>();
builder.Services.AddSingleton<DomainListLoader>();
builder.Services.AddSingleton<FirewallRuleManager>();
builder.Services.AddSingleton<IBlockEngine, FirewallBlockEngine>();
builder.Services.AddSingleton<QuotaTracker>();
builder.Services.AddSingleton<StateMachine>();
builder.Services.AddSingleton<IpcServer>();
builder.Services.AddHostedService<ServiceHost>();

var host = builder.Build();
await host.RunAsync();
