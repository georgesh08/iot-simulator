using System.Text;
using RabbitMQ.Client;

namespace IntegrationTests;

[TestFixture]
public class RabbitMqTests
{
    private IConnection _connection;
    private IModel _channel;
    private readonly string[] _queuesToCheck = [
        "instant_analysis_queue", 
        "continuous_analysis_queue"
    ];
    
    [SetUp]
    public void Setup()
    {
        var factory = new ConnectionFactory
        {
            HostName = "rabbitmq",
            Port = 5672,
            UserName = "guest",
            Password = "guest"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
    }
    
    [TearDown]
    public void Cleanup()
    {
        _channel.Close();
        _connection.Close();
    }
    
    [Test]
    public void QueuesShouldExist()
    {
        foreach (var queue in _queuesToCheck)
        {
            Assert.DoesNotThrow(() =>
                {
                    _channel.QueueDeclarePassive(queue);
                }, $"Queue '{queue}' does not exist.");
        }
    }
    
    [Test]
    public void EachQueueShouldHaveAtLeastOneMessage()
    {
        foreach (var queue in _queuesToCheck)
        {
            var result = _channel.BasicGet(queue, autoAck: true);

            Assert.That(result, Is.Not.Null, $"Queue '{queue}' has no messages.");
            
            var message = Encoding.UTF8.GetString(result.Body.ToArray());
            
            Assert.That(message, Is.Not.Null);
        }
    }
}