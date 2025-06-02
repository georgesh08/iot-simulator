using Grpc.Health.V1;
using Grpc.Net.Client;
using IoTServer;

namespace IntegrationTests;

[TestFixture]
public class GrpcTests
{
    private string _controllerServiceAddress;

    [SetUp]
    public void Setup()
    {
        var controllerAddress = Environment.GetEnvironmentVariable("CONTROLLER_HOST") ?? "localhost";
        var controllerPort = Environment.GetEnvironmentVariable("CONTROLLER_PORT") ?? "18686";
        _controllerServiceAddress = $"http://{controllerAddress}:{controllerPort}";
    }
    
    private async Task<HealthCheckResponse.Types.ServingStatus> IsServiceHealthyAsync(string address)
    {
        using var channel = GrpcChannel.ForAddress(address);
        var client = new Health.HealthClient(channel);
        
        var response = await client.CheckAsync(new HealthCheckRequest());
        return response.Status;
    }
    
    [Test]
    public async Task ControllerShouldBeHealthy()
    {
        var status = await IsServiceHealthyAsync(_controllerServiceAddress);
        Assert.That(status, Is.EqualTo(HealthCheckResponse.Types.ServingStatus.Serving), 
            $"Simulator service is NOT healthy. Actual status {status}");
    }
}