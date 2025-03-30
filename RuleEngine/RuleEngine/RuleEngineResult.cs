namespace RuleEngine;

public class RuleEngineResult
{
	public string Message { get; set; }
	public string DeviceId { get; set; }
	public Status EngineVerdict { get; set; }
}

public enum Status
{
	Ok = 0,
	Error = 1,
	Warning = 2,
}
