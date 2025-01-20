namespace Utils;

public class TimestampConverter
{
	public static ulong ConvertToTimestamp(DateTime dateTime)
	{
		var utcDateTime = dateTime.ToUniversalTime();
		
		var timestamp = (ulong)utcDateTime.Subtract(DateTime.UnixEpoch).TotalMilliseconds;

		return timestamp;
	}

	public static DateTime ConvertFromTimestamp(ulong timestamp)
	{
		return DateTime.UnixEpoch.AddMilliseconds(timestamp);
	}
}
