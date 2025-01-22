using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DataAccessLayer.Models;

public class DbDevice
{
	[BsonId]
	[BsonRepresentation(BsonType.String)]
	public Guid Id { get; set; }
    
	public string Name { get; set; }
    
	public DeviceType Type { get; set; }
    
	public DateTime CreatedAt { get; set; }
}

public enum DeviceType
{
	SENSOR,          // Сенсорные устройства (например, датчики температуры, влажности)
	CAMERA,          // Устройства видеонаблюдения
	WEARABLE,        // Носимые устройства (например, умные часы, фитнес-трекеры)
	LIGHTING,        // Умное освещение (например, умные лампочки)
	SECURITY,        // Системы безопасности (например, сигнализации, замки)
	OTHER            // Другие устройства
}
