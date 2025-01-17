namespace Utils;

public class TimestampConverter
{
	static ulong ConvertToTimestamp(DateTime dateTime)
	{
		var utcDateTime = dateTime.ToUniversalTime();
		
		var timestamp = (ulong)utcDateTime.Subtract(DateTime.UnixEpoch).TotalMilliseconds;

		return timestamp;
	}

	static DateTime ConvertFromTimestamp(ulong timestamp)
	{
		return DateTime.UnixEpoch.AddMilliseconds(timestamp);
	}
}
