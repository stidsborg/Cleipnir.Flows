using Cleipnir.Flows.AspNet;
using Cleipnir.Flows.PostgresSql;
using Cleipnir.Flows.Sample.MicrosoftOpen.Clients;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.MessageDriven.Other;
using Cleipnir.Flows.Sample.MicrosoftOpen.Flows.Rpc;
using Cleipnir.ResilientFunctions.PostgreSQL;
using Serilog;

namespace Cleipnir.Flows.Sample.MicrosoftOpen;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();
        
        var builder = WebApplication.CreateBuilder(args);
        
        builder.Host.UseSerilog();
        builder.Services.AddScoped<OrderFlow>();
        builder.Services.AddSingleton<IEmailClient, EmailClientStub>();
        builder.Services.AddSingleton<ILogisticsClient, LogisticsClientStub>();
        builder.Services.AddSingleton<IPaymentProviderClient, PaymentProviderClientStub>();
        
        const string connectionString = "Server=localhost;Port=5432;Userid=postgres;Password=Pa55word!;Database=flows;";
        await DatabaseHelper.CreateDatabaseIfNotExists(connectionString); //use to create db initially or clean existing state in database
        
        builder.Services.AddFlows(c => c
            .UsePostgresStore(connectionString)
            .WithOptions(new Options(leaseLength: TimeSpan.FromSeconds(5), messagesDefaultMaxWaitForCompletion: TimeSpan.MaxValue))
            .RegisterFlowsAutomatically()
        );

        builder.Services.AddInMemoryBus();
        
        // Add services to the container.
        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();

        app.MapControllers();

        await app.RunAsync();
    }
}