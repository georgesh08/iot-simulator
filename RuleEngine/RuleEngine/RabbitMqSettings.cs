namespace MessageQuery.RabbitMQ;

public class RabbitMqSettings
{
	public string HostName { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME") ?? "localhost";
	public int HostPort{ get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_HOSTPORT") != null 
		? Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_HOSTPORT")) 
		: 5672;
	public string ExchangeName { get; set; } = "iot_exchange";
	public string DeviceQueue { get; set; } = "device_data_queue";
	public string InstantAnalysisQueue { get; set; } = "instant_analysis_queue";
	public string ContinuousAnalysisQueue { get; set; } = "continuous_analysis_queue";
	public string RoutingKey { get; set; } = "iot_device_data";
}
