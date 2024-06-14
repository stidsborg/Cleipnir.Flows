using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Cleipnir.Flows.AspNet;

public class FlowsHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly IEnumerable<Type> _flowsTypes;
    private readonly bool _gracefulShutdown;

    public FlowsHostedService(IServiceProvider services, IEnumerable<Type> flowsTypes, bool gracefulShutdown)
    {
        _services = services;
        _flowsTypes = flowsTypes;
        _gracefulShutdown = gracefulShutdown;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var flowsType in _flowsTypes)
            _ = _services.GetService(flowsType); //flow is registered with the flow container when resolved

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