using System.Text.Json;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Messaging;
using Cleipnir.ResilientFunctions.Reactive.Extensions;
using Confluent.Kafka;

namespace Cleipnir.Flows.Kafka.Tests;

[TestClass]
public sealed class MessageTests
{
    [TestMethod]
    public async Task MultipleMessagesCanBeHandled()
    {
        var instanceId = "Instance#1".ToFlowInstance();
        var flowsContainer = FlowsContainer.Create();
        var flows = flowsContainer.RegisterAnonymousFlow(
            flowFactory: () => new TestFlow()
        );
        var scheduled = await flows.Schedule(instanceId);
        
        var topic = $"topic-{Guid.NewGuid():N}";
        using var producer = ProduceMessages(topic, instanceId);
        _ = ConsumeMessages(
            batchSize: 10,
            topic,
            handler: messages =>
                flows.SendMessages(messages
                    .Select(msg => new BatchedMessage(msg.Instance, msg))
                    .ToList()
                )
        );
        
        await scheduled.Completion(maxWait: TimeSpan.FromSeconds(15));
    }

    private class TestFlow : Flow
    {
        public override async Task Run()
        {
            await Messages
                .OfType<TestMessage>()
                .Take(10)
                .Completion();
        }
    }

    private IProducer<Null, string> ProduceMessages(string topic, FlowInstance instance)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = "localhost:9092" // Kafka broker address
        };

        var producer = new ProducerBuilder<Null, string>(config).Build();
        for (int i = 0; i < 10; i++)
            producer.Produce(
                topic,
                new Message<Null, string> { Value = JsonSerializer.Serialize(new TestMessage(instance.Value, $"Message#{i}")) }
            );

        return producer;
    }

    private async Task ConsumeMessages(int batchSize, string topic, Func<List<TestMessage>, Task> handler)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = "localhost:9092", // Kafka broker address
            GroupId = "test-consumer-group", // Consumer group ID
            AutoOffsetReset = AutoOffsetReset.Earliest // Start from the earliest message
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
        consumer.Subscribe(topic);
        
        var messages = new List<TestMessage>();
        while (messages.Count < batchSize)
        {
            try
            {
                var consumeResult = consumer.Consume(1000);
                if (consumeResult == null && messages.Count < batchSize)
                {
                    Thread.Sleep(250);
                    continue;
                }

                var json = consumeResult!.Message.Value;
                var testMessage = JsonSerializer.Deserialize<TestMessage>(json);
                messages.Add(testMessage!);
            }
            catch (ConsumeException e)
            {
                if (e.Error.Code == ErrorCode.UnknownTopicOrPart)
                    Thread.Sleep(250);
            }
        }

        await handler(messages);
        consumer.Commit();
    }
}