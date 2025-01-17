namespace SimulatorServer;

public interface IGrpcService
{
	void Start(CancellationTokenSource tokenSource);
	void Stop();
}
