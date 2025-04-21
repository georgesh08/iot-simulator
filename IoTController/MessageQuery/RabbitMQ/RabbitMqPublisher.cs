using System.Text;
using DataAccessLayer;
using DataAccessLayer.Models;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using Utils;

namespace MessageQuery.RabbitMQ;

public class RabbitMqPublisher : IMessagePublisher, IDisposable
{
	private IConnection connection;
	private IModel channel;
	private readonly RabbitMqSettings settings;
	private readonly PeriodicalScheduler rmqReconnectScheduler;
	private readonly ConnectionFactory connectionFactory;
	private bool canSubscribeToQueues;
	private int reconnectAttempts;
	private const int MaxReconnectAttempts = 40;
	
	private readonly IDatabaseService databaseService;

	public RabbitMqPublisher(IDatabaseService dbService, RabbitMqSettings? settings = null)
	{
		this.settings = settings ?? new RabbitMqSettings();
		
		connectionFactory = new ConnectionFactory
		{
			HostName = this.settings.HostName
		};

		databaseService = dbService;
		
		rmqReconnectScheduler = new PeriodicalScheduler(TryConnectToRmq, TimeSpan.FromSeconds(3));
		rmqReconnectScheduler.Start();
	}

	public void SubscribeToAnalysisResults()
	{
		if (channel == null)
		{
			Log.Error("RabbitMQ channel is not initialized. Skipping subscription.");
			return;
		}
		
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

			var res = CreateResult(result);
			res.RuleType = RuleType.Instant;
			
			databaseService.SaveDeviceDataRecordAsync(res);
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
			
			var res = CreateResult(result);
			res.RuleType = RuleType.Continuous;
			
			databaseService.SaveDeviceDataRecordAsync(res);
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
			
			Log.Information("Published device message to queue");

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
	
	public bool CanSubscribeToQueues => canSubscribeToQueues;

	private IBasicProperties CreateProps()
	{
		var properties = channel.CreateBasicProperties();
		properties.Persistent = true;
		properties.MessageId = Guid.NewGuid().ToString();
		properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
		
		return properties;
	}

	private DeviceDataResult CreateResult(RuleEngineResult result)
	{
		return new DeviceDataResult
		{
			Id = Guid.NewGuid(),
			DeviceId = result.DeviceId,
			ResponseTimestamp = TimestampConverter.ConvertToTimestamp(DateTime.UtcNow),
			Verdict = result.EngineVerdict == Status.Ok ? ProcessingVerdict.Ok : ProcessingVerdict.Error,
			VerdictMessage = result.Message
		};
	}

	private void TryConnectToRmq()
	{
		Log.Information($"Establishing connection to RabbitMq, attempt {reconnectAttempts}");
		reconnectAttempts++;

		if (reconnectAttempts >= MaxReconnectAttempts)
		{
			rmqReconnectScheduler.Stop(); // stop if max reconnect attempts reached
			Log.Information("Reached max reconnect attempts for connecting to RabbitMQ.");
		}
		
		try
		{
			connection = connectionFactory.CreateConnection();
			channel = connection.CreateModel();
			
			if (channel == null)
			{
				Log.Error("RabbitMQ channel is not initialized. Skipping subscription.");
				return;
			}

			channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Direct, durable: true);
			channel.QueueDeclare(settings.DeviceQueue, durable: true, exclusive: false, autoDelete: false);
			channel.QueueBind(settings.DeviceQueue, settings.ExchangeName, settings.RoutingKey);
			Log.Information($"Connection to RabbitMq established with {settings.HostName}");
			canSubscribeToQueues = true;
			rmqReconnectScheduler.Stop(); // if managed to connect stop
		}
		catch (Exception e)
		{
			Log.Error("Couldn't establish connection to RabbitMQ service. Exception: {0}", e.Message);
		}
	}
}
