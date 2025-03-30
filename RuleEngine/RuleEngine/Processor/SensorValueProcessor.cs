using IoTServer;
using System.Security.Cryptography;

namespace RuleEngine.Processor;

public class SensorValueProcessor : IDeviceValueProcessor
{
	private const int MinimumDataLength = 8;
	private const int OptimalDataLength = 16;
	private const int MaximumDataLength = 32;
	private const byte HeaderByte = 0xA5;
	private const byte FooterByte = 0x5A;

	private readonly Dictionary<byte, string> sensorTypes = new()
	{
		{ 0x01, "Temperature" },
		{ 0x02, "Humidity" },
		{ 0x03, "Pressure" },
		{ 0x04, "Luminosity" },
		{ 0x05, "Accelerometer" },
		{ 0x06, "GPS" },
		{ 0x07, "Motion" },
		{ 0x08, "Voltage" },
		{ 0x09, "Current" },
		{ 0x0A, "Flow" }
	};

	private readonly Dictionary<string, (double Min, double Max)> valueRanges = new()
	{
		{ "Temperature", (-50, 150) },
		{ "Humidity", (0, 100) },
		{ "Pressure", (300, 1200) },
		{ "Luminosity", (0, 100000) },
		{ "Voltage", (0, 380) },
		{ "Current", (0, 100) },
		{ "Flow", (0, 500) }
	};

	private readonly Dictionary<string, Queue<double>> valueHistory = new();
	private readonly object lockObject = new();

	public RuleEngineResult ProcessDeviceValue(DeviceProducedValue value)
	{
		var data = value.SensorValue;
		var bytes = data.Data?.ToByteArray();

		if (!data.ActiveStatus || bytes == null)
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Error,
				Message = "Device is not active or data is null.",
			};
		}

		switch (bytes.Length)
		{
			case < MinimumDataLength:
				return new RuleEngineResult
				{
					EngineVerdict = Status.Error,
					Message = $"Invalid bytes length: {bytes.Length}. Minimum required: {MinimumDataLength}."
				};
			case > MaximumDataLength:
				return new RuleEngineResult
				{
					EngineVerdict = Status.Ok,
					Message =
						$"Data length exceeds optimal size: {bytes.Length}. Maximum recommended: {MaximumDataLength}."
				};
		}

		if (bytes[0] != HeaderByte || bytes[^1] != FooterByte)
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Error,
				Message = "Invalid data frame format. Header or footer byte mismatch.",
			};
		}

		var sensorTypeCode = bytes[1];
		if (!sensorTypes.TryGetValue(sensorTypeCode, out string sensorType))
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Error,
				Message = $"Unknown sensor type code: 0x{sensorTypeCode:X2}",
			};
		}

		var firmwareVersion = bytes[2];
		if (firmwareVersion < 5)
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Ok,
				Message = $"Outdated firmware version: {firmwareVersion}. Please update to at least version 5.",
			};
		}

		var sensorValue = ExtractSensorValue(bytes, sensorType);

		if (valueRanges.TryGetValue(sensorType, out var range))
		{
			if (sensorValue < range.Min || sensorValue > range.Max)
			{
				return new RuleEngineResult
				{
					EngineVerdict = Status.Error,
					Message =
						$"Sensor value out of range: {sensorValue}. Valid range for {sensorType}: {range.Min}-{range.Max}."
				};
			}
		}

		UpdateValueHistory(sensorType, sensorValue);
		
		if (IsValueAnomaly(sensorType, sensorValue))
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Ok,
				Message = $"Anomaly detected in {sensorType} sensor readings: {sensorValue}, " +
				          $"average value: {GetAverageValue(sensorType)},  " +
				          $"deviation percent: {GetValueDeviation(sensorType, sensorValue) * 100}",
			};
		}
		
		if (!VerifyChecksum(bytes))
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Error,
				Message = $"Invalid checksum. Data integrity check failed. Expected checksum: {CalculateExpectedChecksum(bytes)}"
			};
		}

		if (bytes.Length >= 12)
		{
			var timestamp = BitConverter.ToUInt32(bytes, 6);
			if (timestamp == 0 || timestamp > (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds())
			{
				return new RuleEngineResult
				{
					EngineVerdict = Status.Ok,
					Message = "Invalid timestamp in sensor data.",
				};
			}
		}

		var metrics = CalculateMetrics(bytes, sensorType, sensorValue);

		return new RuleEngineResult
		{
			EngineVerdict = Status.Ok,
			Message = $"{sensorType} sensor data is valid. Metrics are: {metrics}",
		};
	}

	public RuleEngineResult ProcessDeviceValues(IEnumerable<DeviceProducedValue> values)
	{
		var deviceValues = values.ToList();
		if (deviceValues.Count == 0)
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Error,
				Message = "No values provided for processing.",
			};
		}

		var sensorReadings = new Dictionary<string, List<double>>();
		var deviceStats = new Dictionary<string, int>
		{
			{ "TotalDevices", deviceValues.Count },
			{ "ActiveDevices", 0 },
			{ "ValidData", 0 },
			{ "InvalidData", 0 }
		};
		var errors = new List<string>();
		var warnings = new List<string>();

		var dataHash = new HashSet<string>();

		foreach (var value in deviceValues)
		{
			var data = value.SensorValue;
			var bytes = data.Data?.ToByteArray();

			if (!data.ActiveStatus || bytes == null || bytes.Length < MinimumDataLength)
			{
				errors.Add($"Inactive or invalid data length");
				deviceStats["InvalidData"]++;
				continue;
			}

			deviceStats["ActiveDevices"]++;

			var dataDigest = Convert.ToBase64String(MD5.Create().ComputeHash(bytes));
			if (!dataHash.Add(dataDigest))
			{
				warnings.Add("Duplicate data detected");
				continue;
			}

			var sensorTypeCode = bytes[1];
			if (!sensorTypes.TryGetValue(sensorTypeCode, out string sensorType))
			{
				warnings.Add($"Device has unknown sensor type 0x{sensorTypeCode:X2}");
				continue;
			}

			if (!VerifyChecksum(bytes))
			{
				errors.Add("Checksum verification failed");
				deviceStats["InvalidData"]++;
				continue;
			}

			var sensorValue = ExtractSensorValue(bytes, sensorType);

			if (!sensorReadings.ContainsKey(sensorType))
			{
				sensorReadings[sensorType] = new List<double>();
			}

			sensorReadings[sensorType].Add(sensorValue);

			deviceStats["ValidData"]++;
		}

		if (deviceStats["ValidData"] < deviceStats["TotalDevices"] * 0.7)
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Error,
				Message = $"Too many invalid devices: {deviceStats["InvalidData"]}/{deviceStats["TotalDevices"]}. Errors: {errors}",
			};
		}

		var correlationResults = new Dictionary<string, object>();
		foreach (var sensorType in sensorReadings.Keys)
		{
			var vals = sensorReadings[sensorType];
			if (vals.Count < 2)
			{
				continue;
			}

			var min = vals.Min();
			var max = vals.Max();
			var avg = vals.Average();
			var stdDev = CalculateStandardDeviation(vals);
			var range = max - min;

			correlationResults[sensorType] = new
			{
				vals.Count,
				Min = min,
				Max = max,
				Average = avg,
				StandardDeviation = stdDev,
				Range = range,
				ConsistencyLevel = CalculateConsistencyLevel(stdDev, avg)
			};

			if (stdDev > avg * 0.3 && vals.Count >= 3)
			{
				warnings.Add($"High variation in {sensorType} sensors: StdDev={stdDev:F2}, Avg={avg:F2}");
			}
		}

		if (warnings.Count != 0 && errors.Count == 0)
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Warning,
				Message = $"Sensors data valid with warnings. Sensor correlation: {correlationResults}. " +
				          $"Warnings: {warnings}. Device stats: {deviceStats}",
			};
		}

		if (errors.Count != 0)
		{
			var message = errors.Count > deviceValues.Count / 2
				? "Multiple sensor errors detected."
				: "Some sensor errors detected, but majority of data is valid.";

			var verdict = errors.Count > deviceValues.Count / 2
				? Status.Error
				: Status.Warning;

			return new RuleEngineResult
			{
				EngineVerdict = verdict,
				Message = message
			};
		}

		var reliabilityScore = CalculateOverallReliability(deviceStats, correlationResults);

		if (sensorReadings.Keys.Count < 2 && deviceValues.Count >= 5)
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Warning,
				Message = $"Limited sensor type diversity in the system. Device stats: {deviceStats}"
			};
		}

		var sensorCorrelations = AnalyzeSensorCorrelations(sensorReadings);
		if (sensorCorrelations.Count == 0 || !sensorCorrelations.Values.Any(v => Math.Abs(v) > 0.9))
		{
			return new RuleEngineResult
			{
				EngineVerdict = Status.Ok,
				Message = $"All sensor data is valid and consistent. Device stats: {deviceStats}, " +
				          $"Reliability score: {reliabilityScore}, Sensor correlations {sensorCorrelations}, Sensor stats: {correlationResults}",
			};
		}

		var highestCorrelation = sensorCorrelations.OrderByDescending(kv => Math.Abs(kv.Value)).First();

		return new RuleEngineResult
		{
			EngineVerdict = Status.Ok,
			Message =
				$"Unusually high correlation between {highestCorrelation.Key} sensors: {highestCorrelation.Value:F2}"
		};
	}


	private double ExtractSensorValue(byte[] bytes, string sensorType)
	{
		if (bytes.Length < 6)
		{
			return 0;
		}

		const int valueStartPos = 3;

		switch (sensorType)
		{
			case "Temperature":
				return BitConverter.ToInt16(bytes, valueStartPos) / 10.0;
			case "Humidity":
				return bytes[valueStartPos];
			case "Pressure":
				return BitConverter.ToUInt16(bytes, valueStartPos);
			case "Voltage":
			case "Current":
				return BitConverter.ToUInt16(bytes, valueStartPos) / 100.0;
			case "Flow":
				if (bytes.Length < 8)
				{
					return 0;
				}

				return BitConverter.ToUInt32(bytes, valueStartPos) / 1000.0;
			default:
				return BitConverter.ToUInt16(bytes, valueStartPos);
		}
	}

	private void UpdateValueHistory(string sensorType, double value)
	{
		lock (lockObject)
		{
			if (!valueHistory.ContainsKey(sensorType))
			{
				valueHistory[sensorType] = new Queue<double>(10);
			}

			var history = valueHistory[sensorType];
			if (history.Count >= 10)
			{
				history.Dequeue();
			}

			history.Enqueue(value);
		}
	}

	private double GetAverageValue(string sensorType)
	{
		lock (lockObject)
		{
			if (!valueHistory.ContainsKey(sensorType) || valueHistory[sensorType].Count == 0)
			{
				return 0;
			}

			return valueHistory[sensorType].Average();
		}
	}

	private double GetValueDeviation(string sensorType, double currentValue)
	{
		var avg = GetAverageValue(sensorType);
		if (avg == 0)
		{
			return 0;
		}

		return Math.Abs(currentValue - avg) / avg;
	}

	private bool IsValueAnomaly(string sensorType, double value)
	{
		lock (lockObject)
		{
			if (!valueHistory.ContainsKey(sensorType) || valueHistory[sensorType].Count < 3)
			{
				return false;
			}

			var history = valueHistory[sensorType].ToList();
			var avg = history.Average();
			var stdDev = CalculateStandardDeviation(history);

			return Math.Abs(value - avg) > stdDev * 3;
		}
	}

	private bool VerifyChecksum(byte[] bytes)
	{
		if (bytes.Length < 4)
		{
			return false;
		}

		var actualChecksum = bytes[^2];
		var expectedChecksum = CalculateExpectedChecksum(bytes);

		return actualChecksum == expectedChecksum;
	}

	private byte CalculateExpectedChecksum(byte[] bytes)
	{
		var sum = 0;
		for (var i = 0; i < bytes.Length - 2; i++)
		{
			sum += bytes[i];
		}

		return (byte)(sum % 256);
	}

	private Dictionary<string, object> CalculateMetrics(byte[] bytes, string sensorType, double value)
	{
		var metrics = new Dictionary<string, object>
		{
			["DataQuality"] = bytes.Length >= OptimalDataLength ? 100 : bytes.Length * 100 / OptimalDataLength
		};

		switch (sensorType)
		{
			case "Temperature":
				metrics["RelativeComfort"] = CalculateComfortLevel(value);
				break;

			case "Humidity":
				metrics["Dryness"] = 100 - value;
				break;

			case "Pressure":
				metrics["WeatherStability"] = CalculateWeatherStability(value);
				break;
		}

		lock (lockObject)
		{
			if (valueHistory.ContainsKey(sensorType) && valueHistory[sensorType].Count >= 3)
			{
				metrics["ValueConsistency"] = CalculateConsistencyLevel(
					CalculateStandardDeviation(valueHistory[sensorType].ToList()),
					valueHistory[sensorType].Average()
				);
			}
		}

		return metrics;
	}

	private double CalculateComfortLevel(double temperature)
	{
		return temperature switch
		{
			>= 21 and <= 23 => 100,
			< 21 => Math.Max(0, temperature * 100 / 21),
			_ => Math.Max(0, 100 - (temperature - 23) * 100 / 17)
		};
	}

	private double CalculateWeatherStability(double pressure)
	{
		const double normalPressure = 1013.25;
		double deviation = Math.Abs(pressure - normalPressure);
		return Math.Max(0, 100 - deviation / 5);
	}

	private double CalculateStandardDeviation(List<double> values)
	{
		if (values.Count <= 1)
		{
			return 0;
		}

		var avg = values.Average();
		var sumOfSquares = values.Sum(v => (v - avg) * (v - avg));

		return Math.Sqrt(sumOfSquares / (values.Count - 1));
	}

	private string CalculateConsistencyLevel(double stdDev, double average)
	{
		if (average == 0)
		{
			return "Unknown";
		}

		var relativeStdDev = stdDev / Math.Abs(average);

		return relativeStdDev switch
		{
			< 0.05 => "Excellent",
			< 0.1 => "Good",
			< 0.2 => "Moderate",
			< 0.3 => "Fair",
			_ => "Poor"
		};
	}

	private double CalculateOverallReliability(Dictionary<string, int> deviceStats,
		Dictionary<string, object> correlationResults)
	{
		var baseReliability = deviceStats["ValidData"] * 100.0 / deviceStats["TotalDevices"];

		var sensorTypeCount = correlationResults.Count;
		var diversityFactor = Math.Min(1, sensorTypeCount / 3.0);

		var consistencyFactor = 1.0;
		foreach (var consistency in correlationResults.Values.Select(stats => ((dynamic)stats).ConsistencyLevel))
		{
			switch (consistency)
			{
				case "Excellent":
					consistencyFactor *= 1.1;
					break;
				case "Good":
					consistencyFactor *= 1.05;
					break;
				case "Moderate":
					consistencyFactor *= 1.0;
					break;
				case "Fair":
					consistencyFactor *= 0.9;
					break;
				case "Poor":
					consistencyFactor *= 0.8;
					break;
			}
		}

		return Math.Min(100, baseReliability * diversityFactor * consistencyFactor);
	}

	private Dictionary<string, double> AnalyzeSensorCorrelations(Dictionary<string, List<double>> sensorReadings)
	{
		var result = new Dictionary<string, double>();

		var eligibleTypes = sensorReadings.Where(kv => kv.Value.Count >= 3).Select(kv => kv.Key).ToList();
		return eligibleTypes.Count < 2 ? result : result;
	}
}
