using ControllerServer;

namespace IoTController;

public class IoTController
{
	private GrpcControllerServer server;

	public IoTController()
	{
		server = new GrpcControllerServer();
	}
	
	public void LaunchController()
	{
		server.Start();
	}
	
	public async Task Stop()
	{
		await server.StopAsync();
	}
}
