using Cleipnir.Flows.AspNet;
using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Tests.AspNet;

[TestClass]
public class IntegrationTest
{
    private readonly HttpClient _httpClient = new();
    
    [TestMethod]
    public async Task SunshineScenarioInMemory()
    {
        const string hostUrl = "http://localhost:5000";
        var startFlowUrl = $"{hostUrl}/startFlow";
        await SunshineScenario(hostUrl, startFlowUrl, new InMemoryFunctionStore());
    }
    
    [TestMethod]
    public async Task SunshineScenarioSqlServer()
    {
        var store = await SqlServerHelper.CreateAndInitializeStore();
        const string hostUrl = "http://localhost:5001";
        var startFlowUrl = $"{hostUrl}/startFlow";
        await SunshineScenario(hostUrl, startFlowUrl, store);
    }
    
    [TestMethod]
    public async Task SunshineScenarioPostgres()
    {
        var store = await PostgresSqlHelper.CreateAndInitializeStore();
        const string hostUrl = "http://localhost:5002";
        var startFlowUrl = $"{hostUrl}/startFlow";
        await SunshineScenario(hostUrl, startFlowUrl, store);
    }
    
    [TestMethod]
    public async Task SunshineScenarioMariaDb()
    {
        var store = await MariaDbHelper.CreateAndInitializeMySqlStore();
        const string hostUrl = "http://localhost:5003";
        var startFlowUrl = $"{hostUrl}/startFlow";
        await SunshineScenario(hostUrl, startFlowUrl, store);
    }

    private async Task SunshineScenario(string hostUrl, string startFlowUrl, IFunctionStore functionStore)
    {
        var (webApplication, testFlows) = await StartWebserverLocalHost(
            bindings: serviceCollection => serviceCollection.AddTransient<TestFlow>(),
            startFlow: provider => provider
                .GetRequiredService<TestFlows>()
                .Run("someInstance", "someParameter"),
            hostUrl,
            functionStore
        );
        await using var _ = webApplication;

        var response = await _httpClient.PostAsync(startFlowUrl, new StringContent(""));
        Assert.IsTrue(response.IsSuccessStatusCode, "Response status code was not successful");

        var controlPanel = await testFlows.ControlPanel("someInstance");

        Assert.IsNotNull(controlPanel);
        Assert.AreEqual(Status.Succeeded, controlPanel.Status);
    }

    private async Task<(WebApplication, TestFlows)> StartWebserverLocalHost(
        Action<IServiceCollection> bindings,
        Func<IServiceProvider, Task> startFlow,
        string hostUrl, 
        IFunctionStore functionStore
    )
    {
        var builder = WebApplication.CreateBuilder();
        
        bindings(builder.Services);
        builder.Services.AddFlows(c => c
            .UseStore(functionStore)
            .RegisterFlowsAutomatically()
        );

        var app = builder.Build();
        app.MapGet("/", () => "Hello World!");
        app.MapPost("/startFlow", startFlow);

        _ = Task.Run(() => app.RunAsync(hostUrl));

        await Task.Delay(1000);

        var httpClient = new HttpClient();
        for (var i = 0; i < 10; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(hostUrl);
                if (response.IsSuccessStatusCode) break;
            }
            catch (Exception)
            {
                // ignored
            }
            finally
            {
                await Task.Delay(100);
            }
        }

        return (app, app.Services.GetRequiredService<TestFlows>());
    }
}