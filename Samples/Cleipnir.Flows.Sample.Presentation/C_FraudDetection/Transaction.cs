using System;

namespace ConsoleApp.FraudDetection;

public record Transaction(string Id, Guid Sender, Guid Receiver, decimal Amount, DateTime Created);