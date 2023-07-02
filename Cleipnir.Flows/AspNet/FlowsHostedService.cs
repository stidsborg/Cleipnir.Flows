using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cleipnir.Flows.SourceGeneration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cleipnir.Flows.AspNet;

public class FlowsHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly Assembly _callingAssembly;
    private readonly bool _gracefulShutdown;

    public FlowsHostedService(IServiceProvider services, Assembly callingAssembly, bool gracefulShutdown)
    {
        _services = services;
        _callingAssembly = callingAssembly;
        _gracefulShutdown = gracefulShutdown;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var flowTypes = _callingAssembly
            .GetReferencedAssemblies()
            .Select(Assembly.Load)
            .Concat(new[] { _callingAssembly })
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsSubclassOfRawGeneric(typeof(Flows<,,>)) || t.IsSubclassOfRawGeneric(typeof(Flows<,,,>)));

        foreach (var iRegisterRFuncOnInstantiationType in flowTypes)
            _ = _services.GetService(iRegisterRFuncOnInstantiationType); //flow is registered with the flow container when resolved

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        var flowsContainer = _services.GetRequiredService<FlowsContainer>();
        
        var shutdownTask = flowsContainer.ShutdownGracefully();
        return _gracefulShutdown 
            ? shutdownTask 
            : Task.CompletedTask;
    } 
}