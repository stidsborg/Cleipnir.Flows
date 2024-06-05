using System;
using System.Linq;
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

public class Flows<TFlow> where TFlow : Flow
{
    private readonly FlowsContainer _flowsContainer;
    private readonly ParamlessRegistration _registration;

    private readonly Next<TFlow, Unit, Unit> _next;
    private readonly Action<TFlow, Workflow> _workflowSetter;
    private readonly Action<TFlow, States> _stateSetter; 
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _workflowSetter = CreateWorkflowSetter();
        _stateSetter = CreateStateSetter();
        
        _next = CreateCallChain(flowsContainer.ServiceProvider);

        var subscriptions = typeof(TFlow)
            .GetInterfaces()
            .Select(i => i.IsGenericType
                ? new { GenericType = i, OpenGenericType = i.GetGenericTypeDefinition() }
                : null
            )
            .Where(a => a is not null && a.OpenGenericType == typeof(ISubscribeTo<>))
            .Select(a => new { a!.GenericType, SubscriptionType = a.GenericType.GetGenericArguments()[0] })
            //.Select(a => new RoutingInformation())
            .ToList();
        
        _registration = flowsContainer.FunctionRegistry.RegisterParamless(
            flowName,
            PrepareAndRunFlow
            /*new Settings(
                routes: subscriptions.Any()
                    ?
                )*/
        );
        
        
    }

    private Next<TFlow, Unit, Unit> CreateCallChain(IServiceProvider serviceProvider)
    {
        return CallChain.Create<TFlow, Unit, Unit>(
            _flowsContainer.Middlewares,
            async (_, workflow) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = scope.ServiceProvider.GetRequiredService<TFlow>();
                _workflowSetter(flow, workflow);
                _stateSetter(flow, workflow.States);
                                
                await flow.Run();
                
                return Unit.Instance;
            }
        );
    }

    public async Task<ControlPanel?> ControlPanel(string instanceId)
    {
        var controlPanel = await _registration.ControlPanel(instanceId);
        return controlPanel;
    }
    
    protected Task<TState?> GetState<TState>(string functionInstanceId) where TState : WorkflowState, new() 
        => _registration.GetState<TState>(functionInstanceId);
    
    public MessageWriter MessageWriter(string instanceId) 
        => _registration.MessageWriters.For(instanceId);

    public Task Run(string instanceId) 
        => _registration.Invoke(instanceId);

    public Task Schedule(string instanceId)
        => _registration.Schedule(instanceId);
    
    public Task ScheduleAt(string instanceId, DateTime delayUntil) => _registration.ScheduleAt(instanceId, delayUntil);
    public Task ScheduleIn(string functionInstanceId, TimeSpan delay) => _registration.ScheduleIn(functionInstanceId, delay);
    
    private async Task<Result<Unit>> PrepareAndRunFlow(Workflow workflow) 
        => await _next(Unit.Instance, workflow);
    
    private Action<TFlow, States> CreateStateSetter()
    {
        var iHaveStateType = typeof(TFlow)
            .GetInterfaces()
            .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHaveState<>));

        if (iHaveStateType == null)
            return (_, _) => { };

        var stateType = iHaveStateType.GenericTypeArguments[0];
        
        //fetch state
        var methodInfo = typeof(States)
            .GetMethods()
            .Single(m => m.Name == nameof(States.CreateOrGet) && !m.GetParameters().Any());

        var genericMethodInfo = methodInfo.MakeGenericMethod(stateType);
        var statePropertyInfo = iHaveStateType.GetProperty("State");
        
        return (flow, states) =>
        {
            var state = genericMethodInfo.Invoke(states, parameters: null);
            statePropertyInfo!.SetValue(flow, state);
        };
    }
    
    private Action<TFlow, Workflow> CreateWorkflowSetter()
    {
        ParameterExpression flowParam = Expression.Parameter(typeof(TFlow), "flow");
        ParameterExpression contextParam = Expression.Parameter(typeof(Workflow), "workflow");
        MemberExpression propertyExpr = Expression.Property(flowParam, nameof(Flow.Workflow));
                
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

public class Flows<TFlow, TParam> 
    where TFlow : Flow<TParam>
    where TParam : notnull
{
    private readonly FlowsContainer _flowsContainer;
    private readonly FuncRegistration<TParam, Unit> _registration;

    private readonly Next<TFlow, TParam, Unit> _next;
    private readonly Action<TFlow, Workflow> _workflowSetter;
    private readonly Action<TFlow, States> _stateSetter; 
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _workflowSetter = CreateWorkflowSetter();
        _stateSetter = CreateStateSetter();
        
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
                _stateSetter(flow, workflow.States);
                                
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
    
    protected Task<TState?> GetState<TState>(string functionInstanceId) where TState : WorkflowState, new() 
        => _registration.GetState<TState>(functionInstanceId);
    
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
    
    private Action<TFlow, States> CreateStateSetter()
    {
        var iHaveStateType = typeof(TFlow)
            .GetInterfaces()
            .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHaveState<>));

        if (iHaveStateType == null)
            return (_, _) => { };

        var stateType = iHaveStateType.GenericTypeArguments[0];
        
        //fetch state
        var methodInfo = typeof(States)
            .GetMethods()
            .Single(m => m.Name == nameof(States.CreateOrGet) && !m.GetParameters().Any());

        var genericMethodInfo = methodInfo.MakeGenericMethod(stateType);
        var statePropertyInfo = iHaveStateType.GetProperty("State");
        
        return (flow, states) =>
        {
            var state = genericMethodInfo.Invoke(states, parameters: null);
            statePropertyInfo!.SetValue(flow, state);
        };
    }
    
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
    private readonly Action<TFlow, States> _stateSetter; 
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _workflowSetter = CreateWorkflowSetter();
        _stateSetter = CreateStateSetter();
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
                _stateSetter(flow, workflow.States);
                
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

    protected Task<TState?> GetState<TState>(string functionInstanceId) where TState : WorkflowState, new() 
        => _registration.GetState<TState>(functionInstanceId);
    
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
    
    private Action<TFlow, States> CreateStateSetter()
    {
        var iHaveStateType = typeof(TFlow)
            .GetInterfaces()
            .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IHaveState<>));

        if (iHaveStateType == null)
            return (_, _) => { };

        var stateType = iHaveStateType.GenericTypeArguments[0];
        
        //fetch state
        var methodInfo = typeof(States)
            .GetMethods()
            .Single(m => m.Name == nameof(States.CreateOrGet) && !m.GetParameters().Any());

        var genericMethodInfo = methodInfo.MakeGenericMethod(stateType);
        var statePropertyInfo = iHaveStateType.GetProperty("State");
        
        return (flow, states) =>
        {
            var state = genericMethodInfo.Invoke(states, parameters: null);
            statePropertyInfo!.SetValue(flow, state);
        };
    }
}