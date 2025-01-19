using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace MessageQuery.RabbitMQ;

public class RabbitMQPublisher : IMessagePublisher, IDisposable
{
	private IConnection connection;
	private IModel channel;
	private const string ExchangeName = "iot.device.data";
	private const string QueueName = "device_data";
	private const string RoutingKey = "device.data";
	private readonly string hostname;

	public RabbitMQPublisher()
	{
		hostname = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME") ?? "localhost";
		
		var factory = new ConnectionFactory
		{
			HostName = hostname
		};
		
		connection = factory.CreateConnection();
		channel = connection.CreateModel();
		
		channel.ExchangeDeclare(ExchangeName, ExchangeType.Direct, durable: true);
		channel.QueueDeclare(QueueName, durable: true, exclusive: false, autoDelete: false);
		channel.QueueBind(QueueName, ExchangeName, RoutingKey);
	}
	
	public async Task PublishDeviceDataAsync(DeviceMessage message)
	{
		var messageBody = JsonConvert.SerializeObject(message);
		var body = Encoding.UTF8.GetBytes(messageBody);

		var properties = CreateProps();
		
		channel.BasicPublish(ExchangeName, RoutingKey, properties, body);
	}
	
	public void Dispose()
	{
		channel.Dispose();
		connection.Dispose();
	}

	private IBasicProperties CreateProps()
	{
		var properties = channel.CreateBasicProperties();
		properties.Persistent = true; // Save messages on disk
		properties.MessageId = Guid.NewGuid().ToString();
		properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		
		return properties;
	}
}
