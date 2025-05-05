using Grpc.Health.V1;
using Grpc.Net.Client;
using IoTServer;

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
    public async Task ControllerShouldBeHealthy()
    {
        var status = await IsServiceHealthyAsync(_controllerServiceAddress);
        Assert.That(status, Is.EqualTo(HealthCheckResponse.Types.ServingStatus.Serving), 
            $"Simulator service is NOT healthy. Actual status {status}");
    }

    [Test]
    public async Task SimulatorShouldBeHealthy()
    {
        var status = await IsServiceHealthyAsync(_simulatorServiceAddress);
        Assert.That(status, Is.EqualTo(HealthCheckResponse.Types.ServingStatus.Serving), 
            $"Simulator service is NOT healthy. Actual status {status}");
    }
    
    [Test]
    public async Task RegisterTestDevice_ShouldReturnOk()
    {
        using var channel = GrpcChannel.ForAddress(_controllerServiceAddress);
        var client = new IoTDeviceService.IoTDeviceServiceClient(channel);

        var request = new DeviceRegisterRequest
        {
            Device = new IoTDevice
            {
                Name = "TestDevice",
                Type = DeviceType.Other
            }
        };
        
        var response = await client.RegisterNewDeviceAsync(request);
        Assert.Multiple(() =>
        {
            Assert.That(response.Status, Is.EqualTo(IoTServer.Status.Ok));
            Assert.That(response.DeviceId, Is.Not.Null);
            Assert.That(response.DeviceId, Is.Not.EqualTo(Guid.Empty.ToString()));
        });
    }
}