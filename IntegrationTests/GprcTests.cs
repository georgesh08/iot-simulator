using Grpc.Health.V1;
using Grpc.Net.Client;

namespace IntegrationTests;

[TestFixture]
public class GrpcTests
{
    private string _controllerServiceAddress;
    private string _simulatorServiceAddress;

    [SetUp]
    public void Setup()
    {
        var controllerAddress = Environment.GetEnvironmentVariable("CONTROLLER_HOST") ?? "localhost";
        var generatorAddress = Environment.GetEnvironmentVariable("SIMULATOR_HOST") ?? "localhost";
        var controllerPort = Environment.GetEnvironmentVariable("CONTROLLER_PORT") ?? "18686";
        var simulatorPort = Environment.GetEnvironmentVariable("SIMULATOR_PORT") ?? "16868";
        _controllerServiceAddress = $"http://{controllerAddress}:{controllerPort}";
        _simulatorServiceAddress = $"http://{generatorAddress}:{simulatorPort}";
    }
    
    private async Task<HealthCheckResponse.Types.ServingStatus> IsServiceHealthyAsync(string address)
    {
        using var channel = GrpcChannel.ForAddress(address);
        var client = new Health.HealthClient(channel);
        
        var response = await client.CheckAsync(new HealthCheckRequest());
        return response.Status;
    }
    
    [Test]
    public async Task Controller_Should_Be_Healthy()
    {
        var status = await IsServiceHealthyAsync(_controllerServiceAddress);
        Assert.That(status, Is.EqualTo(HealthCheckResponse.Types.ServingStatus.Serving), 
            $"Simulator service is NOT healthy. Actual status {status}");
    }

    [Test]
    public async Task Simulator_Should_Be_Healthy()
    {
        var status = await IsServiceHealthyAsync(_simulatorServiceAddress);
        Assert.That(status, Is.EqualTo(HealthCheckResponse.Types.ServingStatus.Serving), 
            $"Simulator service is NOT healthy. Actual status {status}");
    }
}