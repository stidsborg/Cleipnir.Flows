using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows;

public class Flows<TFlow, TParam> 
    where TFlow : Flow<TParam>
    where TParam : notnull
{
    private readonly FlowsContainer _flowsContainer;
    private readonly FuncRegistration<TParam, Unit> _registration;

    private readonly Next<TFlow, TParam, Unit> _next;
    private readonly Action<TFlow, Workflow> _workflowSetter;
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _workflowSetter = CreateWorkflowSetter();
        _next = CreateCallChain(flowsContainer.ServiceProvider);
        
        _registration = flowsContainer.FunctionRegistry.RegisterFunc<TParam, Unit>(
            flowName,
            PrepareAndRunFlow
        );
    }

    private Next<TFlow, TParam, Unit> CreateCallChain(IServiceProvider serviceProvider)
    {
        return CallChain.Create<TFlow, TParam, Unit>(
            _flowsContainer.Middlewares,
            async (param, workflow) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = scope.ServiceProvider.GetRequiredService<TFlow>();
                _workflowSetter(flow, workflow);
                                
                await flow.Run(param);
                
                return Unit.Instance;
            }
        );
    }

    public async Task<ControlPanel<TParam, Unit>?> ControlPanel(string instanceId)
    {
        var controlPanel = await _registration.ControlPanel(instanceId);
        return controlPanel;
    }
    
    public MessageWriter MessageWriter(string instanceId) 
        => _registration.MessageWriters.For(instanceId);

    public Task Run(string instanceId, TParam param) 
        => _registration.Invoke(instanceId, param);

    public Task Schedule(string instanceId, TParam param)
        => _registration.Schedule(instanceId, param);
    
    public Task ScheduleAt(
        string instanceId,
        TParam param,
        DateTime delayUntil
    ) => _registration.ScheduleAt(instanceId, param, delayUntil);

    public Task ScheduleIn(string functionInstanceId,
        TParam param,
        TimeSpan delay
    ) => _registration.ScheduleIn(functionInstanceId, param, delay);
    
    private async Task<Result<Unit>> PrepareAndRunFlow(TParam param, Workflow workflow) 
        => await _next(param, workflow);
    
    private Action<TFlow, Workflow> CreateWorkflowSetter()
    {
        ParameterExpression flowParam = Expression.Parameter(typeof(TFlow), "flow");
        ParameterExpression contextParam = Expression.Parameter(typeof(Workflow), "workflow");
        MemberExpression propertyExpr = Expression.Property(flowParam, nameof(Flow<TParam, Unit>.Workflow));
                
        BinaryExpression assignExpr = Expression.Assign(propertyExpr, contextParam);

        // Create a lambda expression
        Expression<Action<TFlow, Workflow>> lambdaExpr = Expression.Lambda<Action<TFlow, Workflow>>(
            assignExpr,
            flowParam,
            contextParam
        );

        // Compile and invoke the lambda expression
        var setter = lambdaExpr.Compile();
        return setter;
    }
}

public class Flows<TFlow, TParam, TResult> 
    where TFlow : Flow<TParam, TResult>
    where TParam : notnull
{
    private readonly FlowsContainer _flowsContainer;
    private readonly FuncRegistration<TParam, TResult> _registration;

    private readonly Next<TFlow, TParam, TResult> _next;
    
    private readonly Action<TFlow, Workflow> _workflowSetter;
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _workflowSetter = CreateWorkflowSetter();
        _next = CreateCallChain(flowsContainer.ServiceProvider);
        
        _registration = flowsContainer.FunctionRegistry.RegisterFunc<TParam, TResult>(
            flowName,
            (param, workflow) => PrepareAndRunFlow(param, workflow)
        );
    }

    private Next<TFlow, TParam, TResult> CreateCallChain(IServiceProvider serviceProvider)
    {
        return CallChain.Create<TFlow, TParam, TResult>(
            _flowsContainer.Middlewares,
            runFlow: async (param, workflow) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = scope.ServiceProvider.GetRequiredService<TFlow>();
                _workflowSetter(flow, workflow);
                
                var result = await flow.Run(param);
                return result;
            }
        );
    }

    public Task<ControlPanel<TParam, TResult>?> ControlPanel(string instanceId) 
        => _registration.ControlPanel(instanceId);

    public MessageWriter MessageWriter(string instanceId) 
        => _registration.MessageWriters.For(instanceId);

    public Task<TResult> Run(string instanceId, TParam param) 
        => _registration.Invoke(instanceId, param);

    public Task Schedule(string instanceId, TParam param)
        => _registration.Schedule(instanceId, param);

    public Task ScheduleAt(
        string instanceId,
        TParam param,
        DateTime delayUntil
    ) => _registration.ScheduleAt(instanceId, param, delayUntil);

    public Task ScheduleIn(string functionInstanceId,
        TParam param,
        TimeSpan delay
    ) => _registration.ScheduleIn(functionInstanceId, param, delay);
    
    private async Task<Result<TResult>> PrepareAndRunFlow(TParam param, Workflow workflow) 
        => await _next(param, workflow);
    
    private Action<TFlow, Workflow> CreateWorkflowSetter()
    {
        ParameterExpression flowParam = Expression.Parameter(typeof(TFlow), "flow");
        ParameterExpression contextParam = Expression.Parameter(typeof(Workflow), "workflow");
        MemberExpression propertyExpr = Expression.Property(flowParam, nameof(Flow<TParam, Unit>.Workflow));
                
        BinaryExpression assignExpr = Expression.Assign(propertyExpr, contextParam);

        // Create a lambda expression
        Expression<Action<TFlow, Workflow>> lambdaExpr = Expression.Lambda<Action<TFlow, Workflow>>(
            assignExpr,
            flowParam,
            contextParam
        );

        // Compile and invoke the lambda expression
        var setter = lambdaExpr.Compile();
        return setter;
    }
}