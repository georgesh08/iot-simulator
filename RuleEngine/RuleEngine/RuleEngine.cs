using System.Collections.Concurrent;
using System.Text;
using MessageQuery.RabbitMQ;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
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
        
        var factory = new ConnectionFactory
        {
	        HostName = this.settings.HostName
        };
		
        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        
        channel.ExchangeDeclare(this.settings.ExchangeName, ExchangeType.Direct, durable: true);

        channel.QueueDeclare(this.settings.InstantAnalysisQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueDeclare(this.settings.ContinuousAnalysisQueue, durable: true, exclusive: false, autoDelete: false);
        
        channel.QueueBind(this.settings.InstantAnalysisQueue, this.settings.ExchangeName, this.settings.InstantAnalysisQueue);
        channel.QueueBind(this.settings.ContinuousAnalysisQueue, this.settings.ExchangeName, this.settings.ContinuousAnalysisQueue);
        
        continuousRulesScheduler = new PeriodicalScheduler(ProcessContinuousRules, TimeSpan.FromSeconds(10));
    }

    public void Start()
    {
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
		    }
		    else
		    {
			    lastValues[id] = [message];
		    }
	    };
	    
	    channel.BasicConsume(settings.DeviceQueue, autoAck: true, consumer);
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
    }
    
    private void ProcessInstantRules(DeviceMessage message)
    {
	    var verdict = DeviceDataProcessor.ProcessDeviceData(message);
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
