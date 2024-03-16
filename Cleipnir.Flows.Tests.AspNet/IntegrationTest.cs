using Cleipnir.ResilientFunctions.Domain;
using Cleipnir.ResilientFunctions.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cleipnir.Flows.Tests.AspNet;

[TestClass]
public class IntegrationTest
{
    private const string HostUrl = "http://localhost:5000";
    private const string StartFlowUrl = $"{HostUrl}/startFlow";

    private readonly HttpClient _httpClient = new();
    
    [TestMethod]
    public async Task SunshineScenario()
    {
        var (webApplication, testFlows) = await StartWebserverLocalHost(
            bindings: serviceCollection => serviceCollection.AddTransient<TestFlow>(),
            startFlow: provider => provider
                    .GetRequiredService<TestFlows>()
                    .Run("someInstance", "someParameter")
            );
        await using var _ = webApplication;

        var response = await _httpClient.PostAsync(StartFlowUrl, new StringContent(""));
        Assert.IsTrue(response.IsSuccessStatusCode, "Response status code was not successful");

        var controlPanel = await testFlows.ControlPanel("someInstance");

        Assert.IsNotNull(controlPanel);
        Assert.AreEqual(Status.Succeeded, controlPanel.Status);
    }

    private async Task<(WebApplication, TestFlows)> StartWebserverLocalHost(
        Action<IServiceCollection> bindings,
        Func<IServiceProvider, Task> startFlow
    )
    {
        var builder = WebApplication.CreateBuilder();
        bindings(builder.Services);
        Cleipnir.Flows.AspNet.FlowsModule.UseFlows(builder.Services, new InMemoryFunctionStore());

        var app = builder.Build();

        app.MapGet("/", () => "Hello World!");
        app.MapPost("/startFlow", startFlow);

        _ = Task.Run(() => app.RunAsync(HostUrl));

        await Task.Delay(1000);

        var httpClient = new HttpClient();
        for (var i = 0; i < 10; i++)
        {
            try
            {
                var response = await httpClient.GetAsync(HostUrl);
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