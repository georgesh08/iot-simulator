using SimulatorServer;

namespace DataSimulator;

public class DataSimulator
{
	private GrpcSimulatorServer server;
	
	public DataSimulator()
	{
		server = new GrpcSimulatorServer();
	}
	
	public void LaunchSimulator(int numberOfDevices, int dataSendPeriod)
	{
		server.Start();
		
	}
	
	public async Task Stop()
	{
		await server.StopAsync();
	}
}
