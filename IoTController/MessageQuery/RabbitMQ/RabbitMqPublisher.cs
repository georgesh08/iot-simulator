using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;

namespace MessageQuery.RabbitMQ;

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
	private IConnection connection;
	private IModel channel;
	private readonly RabbitMqSettings settings;

	public RabbitMqPublisher(RabbitMqSettings? settings = null)
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

	public void SubscribeToAnalysisResults()
	{
		var instantConsumer = new EventingBasicConsumer(channel);
		var continuousConsumer = new EventingBasicConsumer(channel);

		instantConsumer.Received += (sender, args) =>
		{
			var body = args.Body.ToArray();
			var message = Encoding.UTF8.GetString(body);
			
			var result = JsonConvert.DeserializeObject<RuleEngineResult>(message);
			if (result == null)
			{
				Log.Error("Invalid message received for instant rule");
				return;
			}
			
			Log.Information("Received instant rule result for device {0}. Engine message: {1}. Verdict - {2}", 
				result.DeviceId, 
				result.Message,
				result.EngineVerdict);
		};

		channel.BasicConsume(settings.InstantAnalysisQueue, true, instantConsumer);
		
		continuousConsumer.Received += (sender, args) =>
		{
			var body = args.Body.ToArray();
			var message = Encoding.UTF8.GetString(body);
			
			var result = JsonConvert.DeserializeObject<RuleEngineResult>(message);
			if (result == null)
			{
				Log.Error("Invalid message received for instant rule");
				return;
			}
			
			Log.Information("Received continuous rule result for device {0}. Engine message: {1}. Verdict - {2}", 
				result.DeviceId, 
				result.Message,
				result.EngineVerdict);
		};
		channel.BasicConsume(settings.ContinuousAnalysisQueue, autoAck: true, continuousConsumer);
	}
	
	public async Task PublishDeviceDataAsync(DeviceMessage message)
	{
		try
		{
			if (channel is not { IsOpen: true })
			{
				throw new InvalidOperationException("RabbitMQ channel unavailable.");
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
