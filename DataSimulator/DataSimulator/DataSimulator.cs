using SimulatorServer;

namespace DataSimulator;

public class DataSimulator
{
	private GrpcSimulatorServer server;
	
	public DataSimulator()
	{
		server = new GrpcSimulatorServer();
		
		server.Start();
	}
	
	public void LaunchSimulator(int numberOfDevices, int dataSendPeriod)
	{
		
	}
	
	public async Task Stop()
	{
		await server.StopAsync();
	}
}
