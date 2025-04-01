namespace RuleEngine;

public enum LogLevel
{
	Debug,
	Error,
	Info,
	Warning
}

public record LogMessage(string id, LogLevel level, string message);
