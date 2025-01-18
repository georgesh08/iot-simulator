using Utils;

namespace Base.Device;

public class DeviceNameGenerator(string prefix)
{
	public string GenerateDeviceName()
	{
		var timestamp = TimestampConverter.ConvertToTimestamp(DateTime.Today);

		var postfix = Random.Shared.Next(0, byte.MaxValue);
		
		return $"{prefix}_{timestamp}_{postfix}";
	}
}
