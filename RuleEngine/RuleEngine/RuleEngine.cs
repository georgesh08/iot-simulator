using MessageQuery.RabbitMQ;
using RabbitMQ.Client;

namespace RuleEngine;

public class RuleEngine
{
	private IConnection connection;
	private IModel channel;
	private readonly RabbitMqSettings settings;
    public RuleEngine(RabbitMqSettings? settings = null)
    {
        this.settings = settings ?? new RabbitMqSettings();
        
        var factory = new ConnectionFactory
        {
	        HostName = this.settings.HostName
        };
		
        connection = factory.CreateConnection();
        channel = connection.CreateModel();
        
        channel.ExchangeDeclare(this.settings.ExchangeName, ExchangeType.Direct, durable: true);

        channel.QueueDeclare(this.settings.InstantAnalysisQueue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueDeclare(this.settings.ContinuousAnalysisQueue, durable: true, exclusive: false, autoDelete: false);
    }

    public void Run()
    {
        
    }
}
