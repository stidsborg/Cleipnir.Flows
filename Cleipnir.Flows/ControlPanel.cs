using System;
using System.Threading.Tasks;
using Cleipnir.ResilientFunctions.CoreRuntime;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Helpers;

namespace Cleipnir.Flows;

public class ControlPanel<TParam, TScrapbook> where TParam : notnull where TScrapbook : RScrapbook, new()
{
    private readonly ResilientFunctions.Domain.ControlPanel<TParam, TScrapbook, Unit> _controlPanel;

    public ControlPanel(ResilientFunctions.Domain.ControlPanel<TParam, TScrapbook, Unit> controlPanels)
    {
        _controlPanel = controlPanels;
    }

    public FunctionId FunctionId => _controlPanel.FunctionId;
    public Status Status => _controlPanel.Status;

    public int Epoch => _controlPanel.Epoch;
    public DateTime LeaseExpiration => _controlPanel.LeaseExpiration;

    public Task<ExistingEvents> Events => _controlPanel.Events;
    public ITimeoutProvider TimeoutProvider => _controlPanel.TimeoutProvider;

    public TParam Param
    {
        get => _controlPanel.Param;
        set => _controlPanel.Param = value;
    }

    public TScrapbook Scrapbook
    {
        get => _controlPanel.Scrapbook;
        set => _controlPanel.Scrapbook = value;
    }

    public DateTime? PostponedUntil => _controlPanel.PostponedUntil;
    public PreviouslyThrownException? PreviouslyThrownException => _controlPanel.PreviouslyThrownException;

    public Task Succeed() => _controlPanel.Succeed(Unit.Instance);

    public Task Postpone(DateTime until) => _controlPanel.Postpone(until);
    public Task Postpone(TimeSpan delay) => Postpone(DateTime.UtcNow + delay);

    public Task Fail(Exception exception) => _controlPanel.Fail(exception);
    public Task SaveChanges() => _controlPanel.SaveChanges();
    public Task Delete() => _controlPanel.Delete();

    public Task RunAgain() => _controlPanel.ReInvoke();
    public Task ScheduleAgain() => _controlPanel.ScheduleReInvoke();

    public Task Refresh() => _controlPanel.Refresh();

    public Task WaitForCompletion() => _controlPanel.WaitForCompletion();

}

public class ControlPanel<TParam, TScrapbook, TReturn> where TParam : notnull where TScrapbook : RScrapbook, new()
{
    private readonly ResilientFunctions.Domain.ControlPanel<TParam, TScrapbook, TReturn> _controlPanel;

    public ControlPanel(ResilientFunctions.Domain.ControlPanel<TParam, TScrapbook, TReturn> controlPanels)
    {
        _controlPanel = controlPanels;
    }

    public FunctionId FunctionId => _controlPanel.FunctionId;
    public Status Status => _controlPanel.Status;

    public int Epoch => _controlPanel.Epoch;
    public DateTime LeaseExpiration => _controlPanel.LeaseExpiration;

    public Task<ExistingEvents> Events => _controlPanel.Events;
    public ITimeoutProvider TimeoutProvider => _controlPanel.TimeoutProvider;

    public TParam Param
    {
        get => _controlPanel.Param;
        set => _controlPanel.Param = value;
    }

    public TScrapbook Scrapbook
    {
        get => _controlPanel.Scrapbook;
        set => _controlPanel.Scrapbook = value;
    }

    public TReturn? Result { get => _controlPanel.Result; set => _controlPanel.Result = value; }
    public DateTime? PostponedUntil => _controlPanel.PostponedUntil;
    public PreviouslyThrownException? PreviouslyThrownException => _controlPanel.PreviouslyThrownException;

    public Task Succeed(TReturn result) => _controlPanel.Succeed(result); 

    public Task Postpone(DateTime until) => _controlPanel.Postpone(until);
    public Task Postpone(TimeSpan delay) => Postpone(DateTime.UtcNow + delay);

    public Task Fail(Exception exception) => _controlPanel.Fail(exception);
    public Task SaveChanges() => _controlPanel.SaveChanges();
    public Task Delete() => _controlPanel.Delete();

    public Task<TReturn> RunAgain() => _controlPanel.ReInvoke();
    public Task ScheduleAgain() => _controlPanel.ScheduleReInvoke();

    public Task Refresh() => _controlPanel.Refresh();

    public Task<TReturn> WaitForCompletion() => _controlPanel.WaitForCompletion();
}