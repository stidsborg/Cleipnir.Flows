﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Cleipnir.Flows.CrossCutting;
using Cleipnir.ResilientFunctions;
using Cleipnir.ResilientFunctions.CoreRuntime.Invocation;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows;

public interface IBaseFlows
{
    public static abstract Type FlowType { get; }

    public Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null) where T : notnull;
    public Task<IReadOnlyList<StoredInstance>> GetInstances(Status? status = null);
}

public abstract class BaseFlows<TFlow> : IBaseFlows where TFlow : notnull
{
    public static Type FlowType { get; } = typeof(TFlow);

    private FlowsContainer FlowsContainer { get; }
    private readonly Func<TFlow>? _flowFactory;

    protected BaseFlows(FlowsContainer flowsContainer, Func<TFlow>? flowFactory)
    {
        FlowsContainer = flowsContainer;
        _flowFactory = flowFactory;
    } 
    
    public abstract Task<IReadOnlyList<StoredInstance>> GetInstances(Status? status = null);
    public abstract Task Interrupt(IEnumerable<StoredInstance> instances);
    
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
    
    protected Next<TFlow, TParam, TResult> CreateMiddlewareCallChain<TParam, TResult>(Func<TFlow, TParam, Task<TResult>> runFlow) where TParam : notnull
    {
        var serviceProvider = FlowsContainer.ServiceProvider;
        var workflowSetter = CreateWorkflowSetter();
        return CallChain.Create<TFlow, TParam, TResult>(
            FlowsContainer.Middlewares,
            runFlow: async (param, workflow) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = _flowFactory == null
                    ? scope.ServiceProvider.GetRequiredService<TFlow>()
                    : _flowFactory();
                
                workflowSetter(flow, workflow);
                
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

    public Flows(string flowName, FlowsContainer flowsContainer, FlowOptions? options = null, Func<TFlow>? flowFactory = null) : base(flowsContainer, flowFactory)
    {
        var callChain = CreateMiddlewareCallChain<Unit, Unit>(runFlow: async (flow, _) =>
        {
            await flow.Run();
            return Unit.Instance;
        });
        
        flowsContainer.EnsureNoExistingRegistration(flowName, typeof(TFlow));
        _registration = flowsContainer.FunctionRegistry.RegisterParamless(
            flowName,
            inner: workflow => callChain(Unit.Instance, workflow),
            (options ?? FlowOptions.Default).MapToLocalSettings()
        );
    }

    public async Task<ControlPanel?> ControlPanel(FlowInstance instanceId)
    {
        var controlPanel = await _registration.ControlPanel(instanceId);
        return controlPanel;
    }
    
    protected Task<TState?> GetState<TState>(FlowInstance instanceId) where TState : FlowState, new() 
        => _registration.GetState<TState>(instanceId);
    
    public MessageWriter MessageWriter(FlowInstance instanceId) 
        => _registration.MessageWriters.For(instanceId);

    public Task Run(FlowInstance instanceId, InitialState? initialState = null) 
        => _registration.Invoke(instanceId, initialState);

    public Task<Scheduled> Schedule(FlowInstance instanceId, InitialState? initialState = null)
        => _registration.Schedule(instanceId, initialState: initialState);
    
    public Task ScheduleAt(FlowInstance instanceId, DateTime delayUntil) => _registration.ScheduleAt(instanceId, delayUntil);
    public Task ScheduleIn(FlowInstance instanceId, TimeSpan delay) => _registration.ScheduleIn(instanceId.Value, delay);

    public override Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null) 
        => _registration.RouteMessage(message, correlationId, idempotencyKey);

    public Task<BulkScheduled> BulkSchedule(IEnumerable<FlowInstance> instanceIds) => _registration.BulkSchedule(instanceIds);

    public override Task<IReadOnlyList<StoredInstance>> GetInstances(Status? status = null) => _registration.GetInstances(status);
    public override Task Interrupt(IEnumerable<StoredInstance> instances) => _registration.Interrupt(instances);

    public Task<Finding> SendMessage<T>(FlowInstance flowInstance, T message, bool create = true, string? idempotencyKey = null) where T : notnull 
        => _registration.SendMessage(flowInstance, message, create, idempotencyKey);
    public Task SendMessages(IReadOnlyList<BatchedMessage> messages, bool interrupt = true) 
        => _registration.SendMessages(messages, interrupt);
}

public class Flows<TFlow, TParam> : BaseFlows<TFlow>
    where TFlow : Flow<TParam>
    where TParam : notnull
{
    private readonly ActionRegistration<TParam> _registration;
    
    public Flows(string flowName, FlowsContainer flowsContainer, FlowOptions? options = null, Func<TFlow>? flowFactory = null) : base(flowsContainer, flowFactory)
    {
        var callChain = CreateMiddlewareCallChain<TParam, Unit>(
            runFlow: async (flow, param) =>
            {
                await flow.Run(param);
                return Unit.Instance;
            });
        
        flowsContainer.EnsureNoExistingRegistration(flowName, typeof(TFlow));
        _registration = flowsContainer.FunctionRegistry.RegisterAction<TParam>(
            flowName,
            inner: (param, workflow) => callChain(param, workflow),
            settings: (options ?? FlowOptions.Default).MapToLocalSettings()
        );
    }

    public async Task<ControlPanel<TParam>?> ControlPanel(FlowInstance instanceId)
    {
        var controlPanel = await _registration.ControlPanel(instanceId);
        return controlPanel;
    }
    
    protected Task<TState?> GetState<TState>(FlowInstance instanceId) where TState : FlowState, new() 
        => _registration.GetState<TState>(instanceId);
    
    public MessageWriter MessageWriter(FlowInstance instanceId) 
        => _registration.MessageWriters.For(instanceId);

    public Task Run(FlowInstance instanceId, TParam param, InitialState? initialState = null) 
        => _registration.Invoke(instanceId, param, initialState);

    public Task<Scheduled> Schedule(FlowInstance instanceId, TParam param, InitialState? initialState = null)
        => _registration.Schedule(instanceId, param, initialState: initialState);
    
    public Task ScheduleAt(
        FlowInstance instanceId,
        TParam param,
        DateTime delayUntil
    ) => _registration.ScheduleAt(instanceId, param, delayUntil);

    public Task ScheduleIn(
        FlowInstance instanceId,
        TParam param,
        TimeSpan delay
    ) => _registration.ScheduleIn(instanceId.Value, param, delay);

    public override Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null)
        => _registration.RouteMessage(message, correlationId, idempotencyKey);

    public override Task<IReadOnlyList<StoredInstance>> GetInstances(Status? status = null) => _registration.GetInstances(status);
    public override Task Interrupt(IEnumerable<StoredInstance> instances) => _registration.Interrupt(instances);

    public Task<Finding> SendMessage<T>(FlowInstance flowInstance, T message, string? idempotencyKey = null) where T : notnull 
        => _registration.SendMessage(flowInstance, message, idempotencyKey);
    public Task SendMessages(IReadOnlyList<BatchedMessage> messages, bool interrupt = true) 
        => _registration.SendMessages(messages, interrupt);

    public Task<BulkScheduled> BulkSchedule(IEnumerable<BulkWork<TParam>> bulkWork) => _registration.BulkSchedule(bulkWork);
}

public class Flows<TFlow, TParam, TResult> : BaseFlows<TFlow>
    where TFlow : Flow<TParam, TResult>
    where TParam : notnull
{
    private readonly FuncRegistration<TParam, TResult> _registration;
    
    public Flows(string flowName, FlowsContainer flowsContainer, FlowOptions? options = null, Func<TFlow>? flowFactory = null) : base(flowsContainer, flowFactory)
    {
        var callChain = CreateMiddlewareCallChain<TParam, TResult>(
            runFlow: (flow, param) => flow.Run(param)
        );
        
        flowsContainer.EnsureNoExistingRegistration(flowName, typeof(TFlow));
        _registration = flowsContainer.FunctionRegistry.RegisterFunc<TParam, TResult>(
            flowName,
            inner: (param, workflow) => callChain(param, workflow),
            (options ?? FlowOptions.Default).MapToLocalSettings()
        );
    }

    public Task<ControlPanel<TParam, TResult>?> ControlPanel(string instanceId) 
        => _registration.ControlPanel(instanceId);

    public MessageWriter MessageWriter(FlowInstance instanceId) 
        => _registration.MessageWriters.For(instanceId);

    public Task<TResult> Run(FlowInstance instanceId, TParam param, InitialState? initialState = null) 
        => _registration.Invoke(instanceId, param, initialState);

    public Task<Scheduled<TResult>> Schedule(FlowInstance instanceId, TParam param, InitialState? initialState = null)
        => _registration.Schedule(instanceId, param, initialState: initialState);

    public Task ScheduleAt(
        FlowInstance instanceId,
        TParam param,
        DateTime delayUntil
    ) => _registration.ScheduleAt(instanceId, param, delayUntil);

    public Task ScheduleIn(
        FlowInstance instanceId,
        TParam param,
        TimeSpan delay
    ) => _registration.ScheduleIn(instanceId.Value, param, delay);

    protected Task<TState?> GetState<TState>(FlowInstance instanceId) where TState : FlowState, new() 
        => _registration.GetState<TState>(instanceId);

    public override Task RouteMessage<T>(T message, string correlationId, string? idempotencyKey = null)
        => _registration.RouteMessage(message, correlationId, idempotencyKey);

    public override Task<IReadOnlyList<StoredInstance>> GetInstances(Status? status = null) => _registration.GetInstances(status);
    public override Task Interrupt(IEnumerable<StoredInstance> instances) => _registration.Interrupt(instances);

    public Task<Finding> SendMessage<T>(FlowInstance flowInstance, T message, string? idempotencyKey = null) where T : notnull 
        => _registration.SendMessage(flowInstance, message, idempotencyKey);
    public Task SendMessages(IReadOnlyList<BatchedMessage> messages, bool interrupt = true) 
        => _registration.SendMessages(messages, interrupt);
    
    public Task<BulkScheduled<TResult>> BulkSchedule(IEnumerable<BulkWork<TParam>> bulkWork) => _registration.BulkSchedule(bulkWork);
}