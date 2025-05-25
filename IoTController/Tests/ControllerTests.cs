using ControllerServer;
using DataAccessLayer;
using DataAccessLayer.Models;
using DataAccessLayer.MongoDb;
using DotNet.Testcontainers.Builders;
using Google.Protobuf;
using IoTServer;
using MessageQuery;
using MessageQuery.RabbitMQ;
using Testcontainers.MongoDb;
using Testcontainers.RabbitMq;
using Utils;
using DeviceType = IoTServer.DeviceType;

namespace Tests;

public class ControllerTests
{
	private MongoDbContainer mongo;
	private RabbitMqContainer rabbitMq;
	private IDatabaseService repository;
#pragma warning disable NUnit1032
	private IMessagePublisher messagePublisher;
#pragma warning restore NUnit1032

	private const string TestUser = "testuser";
	private const string TestPassword = "testpass";
	
	[SetUp]
	public async Task Setup()
	{
		mongo = new MongoDbBuilder()
			.WithUsername(TestUser)
			.WithPassword(TestPassword)
			.WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
			.Build();
            
		rabbitMq = new RabbitMqBuilder()
			.WithImage("rabbitmq:latest")
			.WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Server startup complete"))
			.Build();
        
		await Task.WhenAll(
			mongo.StartAsync(),
			rabbitMq.StartAsync()
		);
		
		repository = new MongoDbService(mongo.GetConnectionString());
		messagePublisher = new RabbitMqPublisher(repository, rabbitMq.GetConnectionString());
	}
	
	[Test]
	public void ShouldSendDataToQueue()
	{
		var id = Guid.NewGuid();
		var value = new DeviceProducedValue
		{
			DummyValue = new DummyDeviceData
			{
				ActiveStatus = true,
				Value1 = 10,
				Value2 = 20
			}
		};

		var message = GetDeviceMessage(id, value);
		Assert.DoesNotThrowAsync(async () =>
		{
			await messagePublisher.PublishDeviceDataAsync(message);
		});
	}

	[Test]
	public async Task CheckDeviceExistsInMongo_ShouldBeFalse()
	{
		var deviceName = "test";

		var res = await repository.DeviceExistsAsync(deviceName);
		
		Assert.That(res, Is.Null);
	}
	
	[Test]
	public void CheckDbDeviceMapping()
	{
		var request = new DeviceRegisterRequest
		{
			Device = GetDummyDevice()
		};
		
		var newDeviceId = Guid.NewGuid();
		
		var dbDevice = MongoEntityMapper.CreateDevice(request, newDeviceId);
		
		Assert.That(DateTime.Now.Date, Is.EqualTo(dbDevice.CreatedAt.Date));
	}
	
	[Test]
	public void CreateDevice_EnsureWritingInDb()
	{
		var request = new DeviceRegisterRequest
		{
			Device = GetDummyDevice()
		};
		
		var newDeviceId = Guid.NewGuid();
		
		var dbDevice = MongoEntityMapper.CreateDevice(request, newDeviceId);
		
		Assert.DoesNotThrowAsync(async () =>
		{
			await repository.CreateDeviceAsync(dbDevice);
		});
	}
	
	[Test]
	public async Task CheckDeviceExistsInMongo_ShouldBeTrue()
	{
		var deviceName = "test1";
		var request = new DeviceRegisterRequest
		{
			Device = new IoTDevice
			{
				Name = deviceName,
				Type = DeviceType.Camera
			}
		};
		
		var newDeviceId = Guid.NewGuid();
		
		var dbDevice = MongoEntityMapper.CreateDevice(request, newDeviceId);
		
		Assert.DoesNotThrowAsync(async () =>
		{
			await repository.CreateDeviceAsync(dbDevice);
		});

		var res = await repository.DeviceExistsAsync(deviceName);

		Assert.That(res, Is.Not.Null);
		Assert.That(res.Id, Is.EqualTo(newDeviceId));
	}
	
	[Test]
	public void EnsureWritingInDbRuleEngineReport()
	{
		var verdict = new RuleEngineResult
		{
			DeviceId = Guid.NewGuid().ToString(),
			EngineVerdict = MessageQuery.Status.Ok,
			Message = "test",
		};

		var res = CreateResult(verdict);
		
		Assert.DoesNotThrowAsync(async () =>
		{
			await repository.SaveDeviceDataRecordAsync(res);
		});
	}
	
	[TearDown]
	public async Task Teardown()
	{
		await mongo.DisposeAsync();
		await rabbitMq.DisposeAsync();
	}
	
	private DeviceMessage GetDeviceMessage(Guid deviceId, DeviceProducedValue value)
	{
		var bytes = value.ToByteArray();
		var res = Convert.ToBase64String(bytes);
		return new DeviceMessage
		{
			DeviceId = deviceId.ToString(),
			Value = res,
			Timestamp = TimestampConverter.ConvertToTimestamp(DateTime.UtcNow),
		};
	}

	private IoTDevice GetDummyDevice()
	{
		return new IoTDevice
		{
			Name = "test",
			Type = DeviceType.Camera
		};
	}
	
	private DeviceDataResult CreateResult(RuleEngineResult result)
	{
		return new DeviceDataResult
		{
			Id = Guid.NewGuid(),
			DeviceId = result.DeviceId,
			ResponseTimestamp = TimestampConverter.ConvertToTimestamp(DateTime.UtcNow),
			Verdict = result.EngineVerdict == MessageQuery.Status.Ok ? ProcessingVerdict.Ok : ProcessingVerdict.Error,
			VerdictMessage = result.Message
		};
	}
}
