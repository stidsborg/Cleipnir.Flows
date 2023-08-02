using System;
using System.Collections.Generic;
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

public class Flows<TFlow, TParam, TScrapbook, TResult> 
    where TFlow : Flow<TParam, TScrapbook, TResult>
    where TScrapbook : RScrapbook, new() 
    where TParam : notnull
{
    private readonly FlowsContainer _flowsContainer;
    private readonly RFunc<TParam, TScrapbook, TResult> _registration;

    private readonly Next<TFlow, TParam, TScrapbook, TResult> _next;
    
    private readonly Action<TFlow, TScrapbook> _scrapbookSetter;
    private readonly Action<TFlow, Context> _contextSetter;
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _scrapbookSetter = CreateScrapbookSetter();
        _contextSetter = CreateContextSetter();
        _next = CreateCallChain(flowsContainer.ServiceProvider);
        
        _registration = flowsContainer.RFunctions.RegisterFunc<TParam, TScrapbook, TResult>(
            flowName,
            (param, scrapbook, context) => PrepareAndRunFlow(param, scrapbook, context)
        );
    }

    private Next<TFlow, TParam, TScrapbook, TResult> CreateCallChain(IServiceProvider serviceProvider)
    {
        return CallChain.Create<TFlow, TParam, TScrapbook, TResult>(
            _flowsContainer.Middlewares,
            runFlow: async (param, scrapbook, context) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = scope.ServiceProvider.GetRequiredService<TFlow>();

                _scrapbookSetter(flow, scrapbook);
                _contextSetter(flow, context);
                
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

    public EventSourceWriter EventSourceWriter(string instanceId) 
        => _registration.EventSourceWriters.For(instanceId);

    public Task<TResult> Run(string instanceId, TParam param, TScrapbook? scrapbook = null, IEnumerable<EventAndIdempotencyKey>? events = null) 
        => _registration.Invoke(instanceId, param, scrapbook, events);

    public Task Schedule(string instanceId, TParam param, TScrapbook? scrapbook = null, IEnumerable<EventAndIdempotencyKey>? events = null)
        => _registration.Schedule(instanceId, param, scrapbook, events);
    
    private async Task<Result<TResult>> PrepareAndRunFlow(TParam param, TScrapbook scrapbook, Context context) 
        => await _next(param, scrapbook, context);
    
    private Action<TFlow, TScrapbook> CreateScrapbookSetter()
    {
        ParameterExpression flowParam = Expression.Parameter(typeof(TFlow), "flow");
        ParameterExpression scrapbookParam = Expression.Parameter(typeof(TScrapbook), "scrapbook");
        MemberExpression propertyExpr = Expression.Property(flowParam, nameof(Flow<TParam, TScrapbook, Unit>.Scrapbook));
                
        BinaryExpression assignExpr = Expression.Assign(propertyExpr, scrapbookParam);

        // Create a lambda expression
        Expression<Action<TFlow, TScrapbook>> lambdaExpr = Expression.Lambda<Action<TFlow, TScrapbook>>(
            assignExpr,
            flowParam,
            scrapbookParam
        );

        // Compile and invoke the lambda expression
        var setter = lambdaExpr.Compile();
        return setter;
    }
    
    private Action<TFlow, Context> CreateContextSetter()
    {
        ParameterExpression flowParam = Expression.Parameter(typeof(TFlow), "flow");
        ParameterExpression contextParam = Expression.Parameter(typeof(Context), "context");
        MemberExpression propertyExpr = Expression.Property(flowParam, nameof(Flow<TParam, TScrapbook, Unit>.Context));
                
        BinaryExpression assignExpr = Expression.Assign(propertyExpr, contextParam);

        // Create a lambda expression
        Expression<Action<TFlow, Context>> lambdaExpr = Expression.Lambda<Action<TFlow, Context>>(
            assignExpr,
            flowParam,
            contextParam
        );

        // Compile and invoke the lambda expression
        var setter = lambdaExpr.Compile();
        return setter;
    }
}

public class Flows<TFlow, TParam, TScrapbook> 
    where TFlow : Flow<TParam, TScrapbook>
    where TScrapbook : RScrapbook, new() 
    where TParam : notnull
{
    private readonly FlowsContainer _flowsContainer;
    private readonly RFunc<TParam, TScrapbook, Unit> _registration;

    private readonly Next<TFlow, TParam, TScrapbook, Unit> _next;
    private readonly Action<TFlow, TScrapbook> _scrapbookSetter;
    private readonly Action<TFlow, Context> _contextSetter;
    
    public Flows(string flowName, FlowsContainer flowsContainer)
    {
        _flowsContainer = flowsContainer;
        _scrapbookSetter = CreateScrapbookSetter();
        _contextSetter = CreateContextSetter();
        _next = CreateCallChain(flowsContainer.ServiceProvider);
        
        _registration = flowsContainer.RFunctions.RegisterFunc<TParam, TScrapbook, Unit>(
            flowName,
            PrepareAndRunFlow
        );
    }

    private Next<TFlow, TParam, TScrapbook, Unit> CreateCallChain(IServiceProvider serviceProvider)
    {
        return CallChain.Create<TFlow, TParam, TScrapbook, Unit>(
            _flowsContainer.Middlewares,
            async (param, scrapbook, context) =>
            {
                await using var scope = serviceProvider.CreateAsyncScope();

                var flow = scope.ServiceProvider.GetRequiredService<TFlow>();
                _scrapbookSetter(flow, scrapbook);
                _contextSetter(flow, context);
                                
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
    
    public EventSourceWriter EventSourceWriter(string instanceId) 
        => _registration.EventSourceWriters.For(instanceId);

    public Task Run(string instanceId, TParam param, TScrapbook? scrapbook = null, IEnumerable<EventAndIdempotencyKey>? events = null) 
        => _registration.Invoke(instanceId, param, scrapbook, events);

    public Task Schedule(string instanceId, TParam param, TScrapbook? scrapbook = null, IEnumerable<EventAndIdempotencyKey>? events = null)
        => _registration.Schedule(instanceId, param, scrapbook, events);
    
    private async Task<Result<Unit>> PrepareAndRunFlow(TParam param, TScrapbook scrapbook, Context context) 
        => await _next(param, scrapbook, context);

    private Action<TFlow, TScrapbook> CreateScrapbookSetter()
    {
        ParameterExpression flowParam = Expression.Parameter(typeof(TFlow), "flow");
        ParameterExpression scrapbookParam = Expression.Parameter(typeof(TScrapbook), "scrapbook");
        MemberExpression propertyExpr = Expression.Property(flowParam, nameof(Flow<TParam, TScrapbook, Unit>.Scrapbook));
                
        BinaryExpression assignExpr = Expression.Assign(propertyExpr, scrapbookParam);

        // Create a lambda expression
        Expression<Action<TFlow, TScrapbook>> lambdaExpr = Expression.Lambda<Action<TFlow, TScrapbook>>(
            assignExpr,
            flowParam,
            scrapbookParam
        );

        // Compile and invoke the lambda expression
        var setter = lambdaExpr.Compile();
        return setter;
    }
    
    private Action<TFlow, Context> CreateContextSetter()
    {
        ParameterExpression flowParam = Expression.Parameter(typeof(TFlow), "flow");
        ParameterExpression contextParam = Expression.Parameter(typeof(Context), "context");
        MemberExpression propertyExpr = Expression.Property(flowParam, nameof(Flow<TParam, TScrapbook, Unit>.Context));
                
        BinaryExpression assignExpr = Expression.Assign(propertyExpr, contextParam);

        // Create a lambda expression
        Expression<Action<TFlow, Context>> lambdaExpr = Expression.Lambda<Action<TFlow, Context>>(
            assignExpr,
            flowParam,
            contextParam
        );

        // Compile and invoke the lambda expression
        var setter = lambdaExpr.Compile();
        return setter;
    }
}