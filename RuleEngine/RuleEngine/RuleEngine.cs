using System.Collections.Concurrent;
using System.Text;
using MessageQuery.RabbitMQ;
using Newtonsoft.Json;
using Prometheus;
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
	private readonly PeriodicalScheduler rmqReconectScheduler;
	private int reconnectAttempts;
	private const int MaxReconnectAttempts = 40;
	private readonly PeriodicalScheduler queueReconnectScheduler;

	private readonly ConnectionFactory connectionFactory;

	private readonly ConcurrentDictionary<Guid, List<DeviceMessage>> lastValues;
	
	private readonly PeriodicalScheduler continuousRulesScheduler;
	
	private readonly Histogram ruleProcessingDuration = Metrics
		.CreateHistogram("engine_processing_duration_seconds", "Histogram of rule processing durations.");
	
    public RuleEngine(RabbitMqSettings? settings = null)
    {
	    if (settings == null)
	    {
		    this.settings = new RabbitMqSettings();
	    }
        
        lastValues = new ConcurrentDictionary<Guid, List<DeviceMessage>>();
        
        connectionFactory = new ConnectionFactory
        {
	        HostName = this.settings.HostName,
	        Port = this.settings.HostPort
        };
        
        continuousRulesScheduler = new PeriodicalScheduler(ProcessContinuousRules, TimeSpan.FromSeconds(10));

        rmqReconectScheduler = new PeriodicalScheduler(TryConnectToRmq, TimeSpan.FromSeconds(3));
        queueReconnectScheduler = new PeriodicalScheduler(TryConnectToQueue, TimeSpan.FromSeconds(3));
    }

    public void Start()
    {
	    rmqReconectScheduler.Start();
	    continuousRulesScheduler.Start();
	    queueReconnectScheduler.Start();
    }

    private void TryConnectToQueue()
    {
	    if (channel == null)
	    {
		    Log.Error("RabbitMQ channel is not initialized. Skipping subscription.");
		    return;
	    }

	    try
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
			    
			    Log.Information($"Received data from {message.DeviceId}");

			    using (ruleProcessingDuration.NewTimer())
			    {
				    ProcessInstantRules(message);
			    }
			    
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
		    queueReconnectScheduler.Stop();
	    }
	    catch (Exception e)
	    {
		    Log.Error("Failed to bind to queue. Exception: {0}", e.Message);
	    }
    }
    
    private void TryConnectToRmq()
    {
	    Log.Information($"Establishing connection to RabbitMq, attempt {reconnectAttempts}");
	    reconnectAttempts++;

	    if (reconnectAttempts >= MaxReconnectAttempts)
	    {
		    rmqReconectScheduler.Stop(); // stop if max reconnect attempts reached
		    Log.Information("Reached max reconnect attempts for connecting to RabbitMQ.");
	    }
	    
	    try
	    {
		    Log.Information("Connecting to RabbitMQ service with host [{0}] and port {1}", settings.HostName, settings.HostPort);
		    connection = connectionFactory.CreateConnection();
		    channel = connection.CreateModel();
		    
		    if (channel == null)
		    {
			    Log.Error("RabbitMQ channel is not initialized. Skipping subscription.");
			    return;
		    }
        
		    channel.ExchangeDeclare(settings.ExchangeName, ExchangeType.Direct, durable: true);

		    channel.QueueDeclare(settings.InstantAnalysisQueue, durable: true, exclusive: false, autoDelete: false);
		    channel.QueueDeclare(settings.ContinuousAnalysisQueue, durable: true, exclusive: false, autoDelete: false);
        
		    channel.QueueBind(settings.InstantAnalysisQueue, settings.ExchangeName, settings.InstantAnalysisQueue);
		    channel.QueueBind(settings.ContinuousAnalysisQueue, settings.ExchangeName, settings.ContinuousAnalysisQueue);
		    
		    Log.Information("Connection to RabbitMQ established.");
		    rmqReconectScheduler.Stop();
	    }
        
	    catch (Exception e)
	    {
		    Log.Error("Couldn't establish connection to RabbitMQ service. Exception: {0}", e.Message);
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

		    RuleEngineResult res;
		    using (ruleProcessingDuration.NewTimer())
		    {
			    res= DeviceDataProcessor.ProcessDeviceData(pair.Value);
		    }
		    
		    res.DeviceId = pair.Key.ToString();
		    Log.Information("Publishing continuous analysis result");
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
	    Log.Information("Publishing instant analysis result");
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
		    Log.Information("Engine result published");
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
