using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccessLayer.Models;

public class DeviceDataResult
{
	[BsonId]
	[BsonRepresentation(BsonType.String)]
	public Guid Id { get; set; }
    
	public string DeviceId { get; set; }
    
	public ProcessingVerdict Verdict { get; set; }
    
	public ulong ResponseTimestamp { get; set; }
	public RuleType RuleType { get; set; }
	public string VerdictMessage { get; set; }
}

public enum ProcessingVerdict
{
	Ok,
	Error
}

public enum RuleType
{
	Instant,
	Continuous
}
