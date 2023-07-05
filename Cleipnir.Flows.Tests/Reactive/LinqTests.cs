using Cleipnir.Flows.Reactive;
using Cleipnir.ResilientFunctions.Domain.Exceptions;
using Cleipnir.ResilientFunctions.Reactive;
using Shouldly;

namespace Cleipnir.Flows.Tests.Reactive;

[TestClass]
public class LinqTests
{
    [TestMethod]
    public void EventsCanBeFilteredByType()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
        
        var nextStringEmitted = sourceTask.OfType<string>().Next();
        nextStringEmitted.IsCompleted.ShouldBeFalse();
            
        source.SignalNext(1);
        nextStringEmitted.IsCompleted.ShouldBeFalse();

        source.SignalNext("hello");

        nextStringEmitted.IsCompleted.ShouldBeTrue();
        nextStringEmitted.Result.ShouldBe("hello");
    }
        
    [TestMethod]
    public void NextOperatorEmitsLastEmittedEventAfterCompletionOfTheStream()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        source.SignalNext(1);
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
            
        var next = sourceTask.Next();
        source.SignalNext(2);
            
        next.IsCompletedSuccessfully.ShouldBeTrue();
        next.Result.ShouldBe(1);
            
        source.SignalNext(3); //should not thrown an error
    }
    
    [TestMethod]
    public void NextOperatorWithSuspensionAndTimeoutSucceedsWithImmediateSignal()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
        
        var nextOrSuspend = sourceTask.OfType<int>().SuspendUntilNext(TimeSpan.FromMilliseconds(250));
        source.SignalNext(1);
        nextOrSuspend.IsCompletedSuccessfully.ShouldBeTrue();
        
        nextOrSuspend.Result.ShouldBe(1);
    }
    
    [TestMethod]
    public async Task NextOperatorWithSuspensionAndTimeoutThrowsExceptionWhenNothingIsSignaled()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);

        await Should.ThrowAsync<SuspendInvocationException>(
            () => sourceTask.OfType<int>().SuspendUntilNext(TimeSpan.FromMilliseconds(10))
        );
    }

    [TestMethod]
    public void ThrownExceptionInOperatorResultsInLeafThrowingSameException()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
        var next = sourceTask.Where(_ => throw new InvalidOperationException("oh no")).Next();
            
        next.IsCompleted.ShouldBeFalse();
        source.SignalNext("hello");
            
        next.IsFaulted.ShouldBeTrue();
        next.Exception!.InnerException.ShouldBeOfType<InvalidOperationException>();
    }
        
    [TestMethod]
    public void SubscriptionWithSkip1CompletesAfterNonSkippedSubscription()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
        var next1 = sourceTask.Next();
        var next2 = sourceTask.Skip(1).Next();
            
        source.SignalNext("hello");
        next1.IsCompletedSuccessfully.ShouldBeTrue();
        next2.IsCompleted.ShouldBeFalse();
        source.SignalNext("world");
        next2.IsCompletedSuccessfully.ShouldBeTrue();
    }
    
    [TestMethod]
    public void OfTwoTypesTest()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        source.SignalNext("hello");
        source.SignalNext(2);
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
        
        {
            var either = sourceTask.OfTypes<string, int>().Next().Result;
            either.ValueSpecified.ShouldBe(Either<string, int>.Value.First);
            either.HasFirst.ShouldBeTrue();
            either.Do(first: s => s.ShouldBe("hello"), second: _ => throw new Exception("Unexpected value"));
            var matched = either.Match(first: s => s.ToUpper(), second: _ => throw new Exception("Unexpected value"));
            matched.ShouldBe("HELLO");
        }

        {
            var either = sourceTask.Skip(1).OfTypes<string, int>().Next().Result;
            either.ValueSpecified.ShouldBe(Either<string, int>.Value.Second);
            either.HasFirst.ShouldBeFalse();
            either.Do(first: _ => throw new Exception("Unexpected value"), second: i => i.ShouldBe(2));
            var matched = either.Match(first: _ => throw new Exception("Unexpected value"), second: i => i.ToString());
            matched.ShouldBe("2");
        }
    }
    
    [TestMethod]
    public void OfThreeTypesTest()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        source.SignalNext("hello");
        source.SignalNext(2);
        source.SignalNext(25L);
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
        
        {
            var either = sourceTask.OfTypes<string, int, long>().Next().Result;
            either.ValueSpecified.ShouldBe(Either<string, int, long>.Value.First);
            either.HasFirst.ShouldBeTrue();
            either.Do(
                first: s => s.ShouldBe("hello"),
                second: _ => throw new Exception("Unexpected value"),
                third: _ => throw new Exception("Unexpected value")
            );
            var matched = either.Match(
                first: s => s.ToUpper(), 
                second: _ => throw new Exception("Unexpected value"),
                third: _ => throw new Exception("Unexpected value"));
            matched.ShouldBe("HELLO");
        }

        {
            var either = sourceTask.Skip(2).OfTypes<string, int, long>().Next().Result;
            either.ValueSpecified.ShouldBe(Either<string, int, long>.Value.Third);
            either.HasFirst.ShouldBeFalse();
            either.Do(
                first: _ => throw new Exception("Unexpected value"),
                second: _ => throw new Exception("Unexpected value"),
                third: i => i.ShouldBe(25L)
            );
            var matched = either.Match(
                first: _ => throw new Exception("Unexpected value"),
                second: _ => throw new Exception("Unexpected value"),
                third: i => i.ToString()
            );
            matched.ShouldBe("25");
        }
    }

    [TestMethod]
    public async Task BufferOperatorTest()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        source.SignalNext("hello");
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
        
        var nextTask = sourceTask.Buffer(2).Next();
        var listTask = sourceTask.Buffer(2).ToList();
        
        nextTask.IsCompleted.ShouldBeFalse();
        listTask.IsCompleted.ShouldBeFalse();
        source.SignalNext("world");
        
        nextTask.IsCompletedSuccessfully.ShouldBeTrue();
        var result = await nextTask;
        result.Count.ShouldBe(2);
        result[0].ShouldBe("hello");
        result[1].ShouldBe("world");

        source.SignalNext("hello");
        source.SignalNext("universe");
        source.SignalCompletion();
        
        listTask.IsCompletedSuccessfully.ShouldBeTrue();
        var list = await listTask;
        list.Count.ShouldBe(2);
        var flatten = list.SelectMany(_ => _).ToList();
        flatten.Count.ShouldBe(4);
        flatten[0].ShouldBe("hello");
        flatten[1].ShouldBe("world");
        flatten[2].ShouldBe("hello");
        flatten[3].ShouldBe("universe");
    }
    
    [TestMethod]
    public async Task BufferOperatorOnCompletionEmitsBufferContent()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        source.SignalNext("hello");
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);

        var nextTask = sourceTask.Buffer(2).Next();
        
        source.SignalCompletion();
        nextTask.IsCompletedSuccessfully.ShouldBeTrue();
        var emitted = await nextTask;
        emitted.Count.ShouldBe(1);
        emitted[0].ShouldBe("hello");
    }
    
    [TestMethod]
    public async Task ExistingPropertyContainsPreviouslyEmittedEvents()
    {
        var source = new Source(NoOpTimeoutProvider.Instance);
        source.SignalNext("hello");
        var sourceTask = Task.FromResult((IReactiveChain<object>) source);
        
        var existing = await sourceTask.PullExisting();
        existing.Count.ShouldBe(1);
        existing[0].ShouldBe("hello");

        source.SignalNext("world"); 
        
        existing = await sourceTask.PullExisting();
        existing.Count.ShouldBe(2);
        existing[0].ShouldBe("hello");
        existing[1].ShouldBe("world");
    }
}
