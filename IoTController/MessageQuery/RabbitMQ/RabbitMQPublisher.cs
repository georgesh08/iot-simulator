using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using Serilog;

namespace MessageQuery.RabbitMQ;

public class RabbitMQPublisher : IMessagePublisher, IDisposable
{
	private IConnection connection;
	private IModel channel;
	private readonly RabbitMqSettings settings;

	public RabbitMQPublisher(RabbitMqSettings? settings = null)
	{
		this.settings = settings ?? new RabbitMqSettings();
		
		var factory = new ConnectionFactory
		{
			HostName = this.settings.HostName
		};
		
		connection = factory.CreateConnection();
		channel = connection.CreateModel();
		
		channel.ExchangeDeclare(this.settings.ExchangeName, ExchangeType.Direct, durable: true);
		channel.QueueDeclare(this.settings.DeviceQueue, durable: true, exclusive: false, autoDelete: false);
		channel.QueueBind(this.settings.DeviceQueue, this.settings.ExchangeName, this.settings.RoutingKey);
	}
	
	public async Task PublishDeviceDataAsync(DeviceMessage message)
	{
		try
		{
			if (channel is not { IsOpen: true })
			{
				throw new InvalidOperationException("Канал RabbitMQ не доступен.");
			}

			var messageBody = JsonConvert.SerializeObject(message);
			var body = Encoding.UTF8.GetBytes(messageBody);

			var properties = CreateProps();

			channel.BasicPublish(settings.ExchangeName, settings.RoutingKey, properties, body);

			await Task.CompletedTask;
		}
		catch (Exception ex)
		{
			Log.Error("[{0}] Error on message sending: {1}", ToString(), ex.Message);
		}
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
