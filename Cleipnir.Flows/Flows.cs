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

public interface IBaseFlows
{
    public static abstract Type FlowType { get; }

    public Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null) where T : notnull;
}

public abstract class BaseFlows<TFlow> : IBaseFlows where TFlow : notnull
{
    public static Type FlowType { get; } = typeof(TFlow);

    private FlowsContainer FlowsContainer { get; }
    
    protected BaseFlows(FlowsContainer flowsContainer) => FlowsContainer = flowsContainer;

    private static Action<TFlow, Workflow> CreateWorkflowSetter()
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

    private static Action<TFlow, States> CreateStateSetter()
    {
        var iExposeStateType = typeof(TFlow)
            .GetInterfaces()
            .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IExposeState<>));

        if (iExposeStateType == null)
            return (_, _) => { };

        var stateType = iExposeStateType.GenericTypeArguments[0];
        
        //fetch state
        var methodInfo = typeof(States)
            .GetMethods()
            .Single(m => m.Name == nameof(States.CreateOrGet) && !m.GetParameters().Any());

        var genericMethodInfo = methodInfo.MakeGenericMethod(stateType);
        var statePropertyInfo = iExposeStateType.GetProperty("State");
        
        return (flow, states) =>
        {
            var state = genericMethodInfo.Invoke(states, parameters: null);
            statePropertyInfo!.SetValue(flow, state);
        };
    }
    
    protected Next<TFlow, TParam, TResult> CreateMiddlewareCallChain<TParam, TResult>(Func<TFlow, TParam, Task<TResult>> runFlow) where TParam : notnull
    {
        var serviceProvider = FlowsContainer.ServiceProvider;
        var workflowSetter = CreateWorkflowSetter();
        var stateSetter = CreateStateSetter();
        return CallChain.Create<TFlow, TParam, TResult>(
            FlowsContainer.Middlewares,
            runFlow: async (param, workflow) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = scope.ServiceProvider.GetRequiredService<TFlow>();
                workflowSetter(flow, workflow);
                stateSetter(flow, workflow.States);
                
                var result = await runFlow(flow, param);
                return result;
            }
        );
    }

    public abstract Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null) where T : notnull;
}

public class Flows<TFlow> : BaseFlows<TFlow> where TFlow : Flow
{
    private readonly ParamlessRegistration _registration;

    public Flows(string flowName, FlowsContainer flowsContainer, Options? options = null) : base(flowsContainer)
    {
        var callChain = CreateMiddlewareCallChain<Unit, Unit>(runFlow: async (flow, _) =>
        {
            await flow.Run();
            return Unit.Instance;
        });
        
        _registration = flowsContainer.FunctionRegistry.RegisterParamless(
            flowName,
            inner: workflow => callChain(Unit.Instance, workflow),
            (options ?? Options.Default)
                .MapToRFunctionsSettings()
        );
    }

    public async Task<ControlPanel?> ControlPanel(string instanceId)
    {
        var controlPanel = await _registration.ControlPanel(instanceId);
        return controlPanel;
    }
    
    protected Task<TState?> GetState<TState>(string functionInstanceId) where TState : FlowState, new() 
        => _registration.GetState<TState>(functionInstanceId);
    
    public MessageWriter MessageWriter(string instanceId) 
        => _registration.MessageWriters.For(instanceId);

    public Task Run(string instanceId) 
        => _registration.Invoke(instanceId);

    public Task Schedule(string instanceId)
        => _registration.Schedule(instanceId);
    
    public Task ScheduleAt(string instanceId, DateTime delayUntil) => _registration.ScheduleAt(instanceId, delayUntil);
    public Task ScheduleIn(string functionInstanceId, TimeSpan delay) => _registration.ScheduleIn(functionInstanceId, delay);

    public override Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null) 
        => _registration.RouteMessage(message, correlationId, idempotencyKey);
    public Task<Finding> SendMessage<T>(FlowInstance flowInstance, T message, bool create = true, string? idempotencyKey = null) where T : notnull 
        => _registration.SendMessage(flowInstance, message, create, idempotencyKey);
}

public class Flows<TFlow, TParam> : BaseFlows<TFlow>
    where TFlow : Flow<TParam>
    where TParam : notnull
{
    private readonly FuncRegistration<TParam, Unit> _registration;
    
    public Flows(string flowName, FlowsContainer flowsContainer, Options? options = null) : base(flowsContainer)
    {
        var callChain = CreateMiddlewareCallChain<TParam, Unit>(
            runFlow: async (flow, param) =>
            {
                await flow.Run(param);
                return Unit.Instance;
            });
        
        _registration = flowsContainer.FunctionRegistry.RegisterFunc<TParam, Unit>(
            flowName,
            inner: (param, workflow) => callChain(param, workflow),
            settings: (options ?? Options.Default)
                .MapToRFunctionsSettings()
        );
    }

    public async Task<ControlPanel<TParam, Unit>?> ControlPanel(string instanceId)
    {
        var controlPanel = await _registration.ControlPanel(instanceId);
        return controlPanel;
    }
    
    protected Task<TState?> GetState<TState>(string functionInstanceId) where TState : FlowState, new() 
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

    public override Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null)
        => _registration.RouteMessage(message, correlationId, idempotencyKey);
    public Task<Finding> SendMessage<T>(FlowInstance flowInstance, T message, string? idempotencyKey = null) where T : notnull 
        => _registration.SendMessage(flowInstance, message, idempotencyKey);
}

public class Flows<TFlow, TParam, TResult> : BaseFlows<TFlow>
    where TFlow : Flow<TParam, TResult>
    where TParam : notnull
{
    private readonly FuncRegistration<TParam, TResult> _registration;
    
    public Flows(string flowName, FlowsContainer flowsContainer, Options? options = null) : base(flowsContainer)
    {
        var callChain = CreateMiddlewareCallChain<TParam, TResult>(
            runFlow: (flow, param) => flow.Run(param)
        );
        
        _registration = flowsContainer.FunctionRegistry.RegisterFunc<TParam, TResult>(
            flowName,
            inner: (param, workflow) => callChain(param, workflow),
            (options ?? Options.Default)
                .MapToRFunctionsSettings()
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

    protected Task<TState?> GetState<TState>(string functionInstanceId) where TState : FlowState, new() 
        => _registration.GetState<TState>(functionInstanceId);

    public override Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null)
        => _registration.RouteMessage(message, correlationId, idempotencyKey);
    public Task<Finding> SendMessage<T>(FlowInstance flowInstance, T message, string? idempotencyKey = null) where T : notnull 
        => _registration.SendMessage(flowInstance, message, idempotencyKey);
}