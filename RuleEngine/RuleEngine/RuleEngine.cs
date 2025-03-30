using System.Collections.Concurrent;
using System.Text;
using MessageQuery.RabbitMQ;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RuleEngine.Processor;
using Serilog;
using Utils;

namespace RuleEngine;

public class RuleEngine
{
	private IConnection connection;
	private IModel channel;
	private readonly RabbitMqSettings settings;

	private readonly ConcurrentDictionary<Guid, List<DeviceMessage>> lastValues;
	
	private PeriodicalScheduler continuousRulesScheduler;
	
    public RuleEngine(RabbitMqSettings? settings = null)
    {
        this.settings = settings ?? new RabbitMqSettings();
        lastValues = new ConcurrentDictionary<Guid, List<DeviceMessage>>();
        
        continuousRulesScheduler = new PeriodicalScheduler(ProcessContinuousRules, TimeSpan.FromSeconds(10));
    }

    public void Start()
    {
	    if (!TryStartRabbitMQService())
	    {
		    Log.Error("Failed to start RabbitMQ service");
		    return;
	    }
	    
	    Log.Information("Successfully started RabbitMQ service");
	    
	    continuousRulesScheduler.Start();
	    var consumer = new EventingBasicConsumer(channel);
	    consumer.Received += (sender, args) =>
	    {
		    var body = args.Body.ToArray();
		    var message = JsonConvert.DeserializeObject<DeviceMessage>(Encoding.UTF8.GetString(body));

		    if (message == null)
		    {
			    return;
		    }

		    ProcessInstantRules(message);
		    var deviceId = Guid.TryParse(message.DeviceId, out var id);
		    if (deviceId)
		    {
			    lastValues.TryGetValue(id, out var devices);
			    if (devices != null)
			    {
				    devices.Add(message);
			    }
			    else
			    {
				    lastValues[id] = [message];
			    }
		    }
	    };
	    
	    channel.BasicConsume(settings.DeviceQueue, autoAck: true, consumer);
    }
    
    private bool TryStartRabbitMQService()
    {
	    var factory = new ConnectionFactory
	    {
		    HostName = settings.HostName,
		    Port = settings.HostPort
	    };
        
	    try
	    {
		    Log.Information("Connecting to RabbitMQ service with host [{0}] and port {1}", settings.HostName, settings.HostPort);
		    connection = factory.CreateConnection();
		    channel = connection.CreateModel();
        
		    channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Direct, durable: true);

		    channel.QueueDeclare(settings.InstantAnalysisQueue, durable: true, exclusive: false, autoDelete: false);
		    channel.QueueDeclare(settings.ContinuousAnalysisQueue, durable: true, exclusive: false, autoDelete: false);
        
		    channel.QueueBind(settings.InstantAnalysisQueue, settings.ExchangeName, settings.InstantAnalysisQueue);
		    channel.QueueBind(settings.ContinuousAnalysisQueue, settings.ExchangeName, settings.ContinuousAnalysisQueue);
		    return true;
	    }
        
	    catch (Exception e)
	    {
		    Log.Error("Couldn't establish connection to RabbitMQ service. Exception: {0}", e.Message);
		    return false;
	    }
    }

    private void ProcessContinuousRules()
    {
	    foreach (var pair in lastValues)
	    {
		    if (pair.Value.Count <= 0)
		    {
			    continue;
		    }

		    var res= DeviceDataProcessor.ProcessDeviceData(pair.Value);
		    res.DeviceId = pair.Key.ToString();
		    PublishAnalysisResult(settings.ContinuousAnalysisQueue, res);
	    }

	    foreach (var pair in lastValues)
	    {
		    var value = pair.Value;
		    if (value.Count > 30)
		    {
			    value.Clear();
		    }
	    }
    }
    
    private void ProcessInstantRules(DeviceMessage message)
    {
	    var verdict = DeviceDataProcessor.ProcessDeviceData(message);
	    verdict.DeviceId = message.DeviceId;
	    PublishAnalysisResult(settings.InstantAnalysisQueue, verdict);
    }
    
    private void PublishAnalysisResult(string queue, RuleEngineResult result)
    {
	    try
	    {
		    if (channel is not { IsOpen: true })
		    {
			    throw new InvalidOperationException("RabbitMQ channel unavailable.");
		    }

		    var messageBody = JsonConvert.SerializeObject(result);
		    var body = Encoding.UTF8.GetBytes(messageBody);

		    var properties = CreateProps();

		    channel.BasicPublish(settings.ExchangeName, queue, properties, body);
	    }
	    catch (Exception ex)
	    {
		    Log.Error("[{0}] Error on message sending: {1}", ToString(), ex.Message);
	    }
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
