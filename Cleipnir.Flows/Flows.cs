using System;
using System.Threading.Tasks;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows;

public class Flows<TFlow, TParam, TScrapbook, TResult> 
    where TFlow : Flow<TParam, TScrapbook, TResult>
    where TScrapbook : RScrapbook, new() 
    where TParam : notnull
{
    private readonly FlowsContainer _flowsContainer;
    private readonly RFunc<TParam, TScrapbook, TResult> _registration;

    private readonly Next<TFlow, TParam, TScrapbook, TResult> _next;
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _registration = flowsContainer._rFunctions.RegisterFunc<TParam, TScrapbook, TResult>(
            flowName,
            (param, scrapbook, context) => PrepareAndRunFlow(param, scrapbook, context)
        );
        _next = CreateCallChain(flowsContainer._serviceProvider);
    }

    private Next<TFlow, TParam, TScrapbook, TResult> CreateCallChain(IServiceProvider serviceProvider)
    {
        return CallChain.Create<TFlow, TParam, TScrapbook, TResult>(
            _flowsContainer._middlewares,
            runFlow: async (param, scrapbook, context) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = scope.ServiceProvider.GetRequiredService<TFlow>();

                typeof(TFlow)
                    .GetProperty(nameof(Flow<TParam, TScrapbook, TResult>.Scrapbook))!
                    .SetValue(flow, scrapbook);
                typeof(TFlow)
                    .GetProperty(nameof(Flow<TParam, TScrapbook, TResult>.Context))!
                    .SetValue(flow, context);

                var eventSource = await context.EventSource;
                typeof(TFlow).GetProperty(nameof(Flow<TParam, TScrapbook, TResult>.EventSource))!.SetValue(flow, eventSource);
                typeof(TFlow).GetProperty(nameof(Flow<TParam, TScrapbook, TResult>.Utilities))!.SetValue(flow, context.Utilities);

                var result = await flow.Run(param);
                return result;
            }
        );

    }

    public async Task<ControlPanel<TParam, TScrapbook, TResult>?> ControlPanel(string instanceId)
    {
        var controlPanel = await _registration.ControlPanel.For(instanceId);
        if (controlPanel == null)
            return null;

        return new ControlPanel<TParam, TScrapbook, TResult>(controlPanel);
    }

    public Task<TResult> Run(string instanceId, TParam param, TScrapbook? scrapbook = null) 
        => _registration.Invoke(instanceId, param, scrapbook);

    public Task Schedule(string instanceId, TParam param, TScrapbook? scrapbook = null)
        => _registration.Schedule(instanceId, param, scrapbook);
    
    private async Task<Result<TResult>> PrepareAndRunFlow(TParam param, TScrapbook scrapbook, Context context) 
        => await _next(param, scrapbook, context);
}

public class Flows<TFlow, TParam, TScrapbook> 
    where TFlow : Flow<TParam, TScrapbook>
    where TScrapbook : RScrapbook, new() 
    where TParam : notnull
{
    private readonly FlowsContainer _flowsContainer;
    private readonly RFunc<TParam, TScrapbook, Unit> _registration;

    private readonly Next<TFlow, TParam, TScrapbook, Unit> _next;
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _registration = flowsContainer._rFunctions.RegisterFunc<TParam, TScrapbook, Unit>(
            flowName,
            PrepareAndRunFlow
        );
        _next = CreateCallChain(flowsContainer._serviceProvider);
    }

    private Next<TFlow, TParam, TScrapbook, Unit> CreateCallChain(IServiceProvider serviceProvider)
    {
        return CallChain.Create<TFlow, TParam, TScrapbook, Unit>(
            _flowsContainer._middlewares,
            async (param, scrapbook, context) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = scope.ServiceProvider.GetRequiredService<TFlow>();

                typeof(TFlow)
                    .GetProperty(nameof(Flow<TParam, TScrapbook, Unit>.Scrapbook))!
                    .SetValue(flow, scrapbook);
                typeof(TFlow)
                    .GetProperty(nameof(Flow<TParam, TScrapbook, Unit>.Context))!
                    .SetValue(flow, context);

                var eventSource = await context.EventSource;
                typeof(TFlow).GetProperty(nameof(Flow<TParam, TScrapbook, Unit>.EventSource))!.SetValue(flow, eventSource);
                typeof(TFlow).GetProperty(nameof(Flow<TParam, TScrapbook, Unit>.Utilities))!.SetValue(flow, context.Utilities);

                await flow.Run(param);
                
                return Unit.Instance;
            }
        );
    }

    public async Task<ControlPanel<TParam, TScrapbook>?> ControlPanel(string instanceId)
    {
        var controlPanel = await _registration.ControlPanel.For(instanceId);
        if (controlPanel == null)
            return null;

        return new ControlPanel<TParam, TScrapbook>(controlPanel);
    }

    public Task Run(string instanceId, TParam param, TScrapbook? scrapbook = null) 
        => _registration.Invoke(instanceId, param, scrapbook);

    public Task Schedule(string instanceId, TParam param, TScrapbook? scrapbook = null)
        => _registration.Schedule(instanceId, param, scrapbook);
    
    private async Task<Result<Unit>> PrepareAndRunFlow(TParam param, TScrapbook scrapbook, Context context) 
        => await _next(param, scrapbook, context);
}