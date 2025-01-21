using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccessLayer.Models;

public class DeviceDataResult
{
	[BsonId]
	[BsonRepresentation(BsonType.ObjectId)]
	public string Id { get; set; }
    
	public string DeviceId { get; set; }
    
	public ProcessingVerdict Verdict { get; set; }
    
	public ulong TimestampResponse { get; set; }
}

public enum ProcessingVerdict
{
	Ok,
	Error
}
