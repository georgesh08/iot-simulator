using IoTServer;

namespace RuleEngine.Processor;

public class IndustrialSystemValueProcessor : IDeviceValueProcessor
{
	private const float MAX_TEMPERATURE = 85.0f;
	private const float MIN_TEMPERATURE = -20.0f;
	private const float MAX_HUMIDITY = 95.0f;
	private const float CRITICAL_CPU_LOAD = 90.0f;
	private const float CRITICAL_MEMORY_USAGE = 95.0f;
	private const float LOW_BATTERY_THRESHOLD = 15.0f;
	private const int MAX_ERROR_COUNT = 50;
	private const int ANOMALY_DETECTION_WINDOW = 10;
	private const float PRESSURE_CHANGE_THRESHOLD = 500.0f;
	public RuleEngineResult ProcessDeviceValue(DeviceProducedValue value)
	{
		var industrialData = value.IndustrialDeviceValue;
		
		if (industrialData == null)
		{
			return new RuleEngineResult
			{
				Message = "Not an industrial device value",
				DeviceId = Guid.Empty.ToString(),
				EngineVerdict = Status.Error
			};
		}
		
		var deviceId = industrialData.SystemId;
		var result = new RuleEngineResult
		{
			DeviceId = deviceId,
			EngineVerdict = Status.Ok
		};
		
		var environmentalCheckTask = Task.Run(() => CheckEnvironmentalReadings(industrialData));
		var systemHealthCheckTask = Task.Run(() => CheckSystemHealth(industrialData));
		var diagnosticsCheckTask = Task.Run(() => CheckDiagnostics(industrialData));
		var metadataCheckTask = Task.Run(() => CheckMetadata(industrialData));
		
		Task.WaitAll(environmentalCheckTask, systemHealthCheckTask, diagnosticsCheckTask, metadataCheckTask);
		
		var errors = new List<string>();
        
		if (!environmentalCheckTask.Result.success)
		{
			errors.Add(environmentalCheckTask.Result.message);
		}

		if (!systemHealthCheckTask.Result.success)
		{
			errors.Add(systemHealthCheckTask.Result.message);
		}

		if (!diagnosticsCheckTask.Result.success)
		{
			errors.Add(diagnosticsCheckTask.Result.message);
		}

		if (!metadataCheckTask.Result.success)
		{
			errors.Add(metadataCheckTask.Result.message);
		}

		if (industrialData.ErrorCount > MAX_ERROR_COUNT)
		{
			errors.Add($"Error count exceeds threshold: {industrialData.ErrorCount} > {MAX_ERROR_COUNT}");
		}
		
		if (errors.Count > 0)
		{
			result.EngineVerdict = Status.Error;
			result.Message = string.Join("; ", errors);
		}
		else
		{
			result.Message = $"All checks passed for device {deviceId}";
		}

		return result;
	}

	public RuleEngineResult ProcessDeviceValues(IEnumerable<DeviceProducedValue> values)
	{
		var valuesList = values.ToList();
        
        if (valuesList.Count == 0)
        {
            return new RuleEngineResult
            {
                Message = "No values to process",
                DeviceId = "unknown",
                EngineVerdict = Status.Error
            };
        }

        var industrialValues = valuesList
            .Where(v => v.IndustrialDeviceValue != null)
            .Select(v => v.IndustrialDeviceValue)
            .ToList();

        if (industrialValues.Count == 0)
        {
            return new RuleEngineResult
            {
                Message = "No industrial device values found",
                DeviceId = "unknown",
                EngineVerdict = Status.Error
            };
        }

        var deviceGroups = industrialValues.GroupBy(d => d.SystemId);
        var errors = new List<string>();
        var deviceIds = new HashSet<string>();

        Parallel.ForEach(deviceGroups, deviceGroup =>
        {
            var deviceId = deviceGroup.Key;
            deviceIds.Add(deviceId);
            var deviceValues = deviceGroup.OrderBy(d => d.Timestamp).ToList();
            
            var trendResults = Task.Run(() => AnalyzeTrends(deviceValues));
            var anomalyResults = Task.Run(() => DetectAnomalies(deviceValues));
            var predictionResults = Task.Run(() => PredictFailures(deviceValues));
            var statusPatternResults = Task.Run(() => AnalyzeStatusPatterns(deviceValues));

            Task.WaitAll(trendResults, anomalyResults, predictionResults, statusPatternResults);
            
            lock (errors)
            {
                if (!trendResults.Result.success)
                {
	                errors.Add($"Device {deviceId}: {trendResults.Result.message}");
                }

                if (!anomalyResults.Result.success)
                {
	                errors.Add($"Device {deviceId}: {anomalyResults.Result.message}");
                }

                if (!predictionResults.Result.success)
                {
	                errors.Add($"Device {deviceId}: {predictionResults.Result.message}");
                }

                if (!statusPatternResults.Result.success)
                {
	                errors.Add($"Device {deviceId}: {statusPatternResults.Result.message}");
                }
            }
        });
        
        var result = new RuleEngineResult
        {
	        DeviceId = deviceIds.Count == 1 ? deviceIds.First() : string.Join(",", deviceIds)
        };

        if (errors.Count > 0)
        {
            result.EngineVerdict = Status.Error;
            result.Message = string.Join("; ", errors);
        }
        else
        {
            result.EngineVerdict = Status.Ok;
            result.Message = $"All trend checks passed for {deviceIds.Count} devices";
        }

        return result;
	}
	
	private (bool success, string message) CheckEnvironmentalReadings(IndustrialSystemData data)
	{
		if (data.Environment == null)
		{
			return (false, "Environmental readings missing");
		}

		var env = data.Environment;
		var errors = new List<string>();
		
		switch (env.Temperature)
		{
			case > MAX_TEMPERATURE:
				errors.Add($"Temperature too high: {env.Temperature}°C > {MAX_TEMPERATURE}°C");
				break;
			case < MIN_TEMPERATURE:
				errors.Add($"Temperature too low: {env.Temperature}°C < {MIN_TEMPERATURE}°C");
				break;
		}
		
		if (env.Humidity > MAX_HUMIDITY)
		{
			errors.Add($"Humidity too high: {env.Humidity}% > {MAX_HUMIDITY}%");
		}
		
		switch (env.Quality)
		{
			case DataQuality.Corrupted:
				errors.Add("Environmental data is corrupted");
				break;
			case DataQuality.Bad:
				errors.Add("Environmental data quality is bad");
				break;
		}
		
		if (env is { Temperature: > 50, Pressure: < 90000 })
		{
			errors.Add($"Critical condition: High temperature ({env.Temperature}°C) with low pressure ({env.Pressure} Pa)");
		}

		return errors.Count > 0 
			? (false, string.Join("; ", errors)) 
			: (true, "Environmental checks passed");
	}
	
	private (bool success, string message) CheckSystemHealth(IndustrialSystemData data)
    {
        if (data.SystemHealth == null)
        {
            return (false, "System health data missing");
        }

        var health = data.SystemHealth;
        var errors = new List<string>();
        
        if (health.CpuLoad > CRITICAL_CPU_LOAD)
        {
            errors.Add($"CPU load critical: {health.CpuLoad}% > {CRITICAL_CPU_LOAD}%");
        }
        
        if (health.MemoryUsage > CRITICAL_MEMORY_USAGE)
        {
            errors.Add($"Memory usage critical: {health.MemoryUsage}% > {CRITICAL_MEMORY_USAGE}%");
        }
        
        if (health.BatteryLevel < LOW_BATTERY_THRESHOLD)
        {
            errors.Add($"Battery level low: {health.BatteryLevel}% < {LOW_BATTERY_THRESHOLD}%");
        }
        
        if (health.ActiveProcesses.Count == 0)
        {
            errors.Add("No active processes running");
        }
        else
        {
	        var requiredProcesses = new[] { "monitor", "controller", "dataLogger" };
	        errors.AddRange(from process in requiredProcesses 
		        where !health.ActiveProcesses.Any(p => p.Contains(process, StringComparison.OrdinalIgnoreCase)) 
		        select $"Critical process '{process}' not running");
        }

        if (health.StatusFlags.Count <= 0)
        {
	        return errors.Count > 0
		        ? (false, string.Join("; ", errors))
		        : (true, "System health checks passed");
        }

        if (health.StatusFlags.TryGetValue("systemState", out var state) && state != "running")
        {
	        errors.Add($"System not in running state: {state}");
        }

        if (health.StatusFlags.TryGetValue("errorState", out var errorState) && errorState == "true")
        {
	        errors.Add("System in error state");
        }

        return errors.Count > 0 
            ? (false, string.Join("; ", errors)) 
            : (true, "System health checks passed");
    }
	
	private (bool success, string message) CheckDiagnostics(IndustrialSystemData data)
    {
        if (data.Diagnostics == null)
        {
            return (true, "No diagnostics data to check");
        }

        var diagnostics = data.Diagnostics;
        var errors = new List<string>();
        
        if (diagnostics.Warnings is { Count: > 0 })
        {
            var criticalWarnings = diagnostics.Warnings
                .Where(w => w.Level == AlertLevel.Critical || w.Level == AlertLevel.Emergency)
                .ToList();

            if (criticalWarnings.Count > 0)
            {
	            errors.Add($"Has {criticalWarnings.Count} critical/emergency warnings");

	            errors.AddRange(
		            criticalWarnings.Take(3)
			            .Select(warning => $"Warning [{warning.AlertId}]: {warning.Source} (Level: {warning.Level})"));
            }
        }
        
        if (diagnostics.SystemParams is { Count: > 0 })
        {
            if (diagnostics.SystemParams.TryGetValue("firmwareStatus", out var firmwareStatus) 
                && firmwareStatus != "current" && firmwareStatus != "up-to-date")
            {
                errors.Add($"Firmware not up to date: {firmwareStatus}");
            }

            if (diagnostics.SystemParams.TryGetValue("calibrationStatus", out var calibrationStatus) 
                && calibrationStatus != "calibrated")
            {
                errors.Add($"System not properly calibrated: {calibrationStatus}");
            }
        }

        if (diagnostics.Logs is not { Count: > 0 })
        {
	        return errors.Count > 0
		        ? (false, string.Join("; ", errors))
		        : (true, "Diagnostic checks passed");
        }

        var errorLogs = diagnostics.Logs
	        .Where(log => log.Contains("ERROR", StringComparison.OrdinalIgnoreCase) 
	                      || log.Contains("CRITICAL", StringComparison.OrdinalIgnoreCase)
	                      || log.Contains("FATAL", StringComparison.OrdinalIgnoreCase))
	        .ToList();

        if (errorLogs.Count > 5)
        {
	        errors.Add($"High number of error logs: {errorLogs.Count} entries");
        }

        return errors.Count > 0 
            ? (false, string.Join("; ", errors)) 
            : (true, "Diagnostic checks passed");
    }
	
	private (bool success, string message) CheckMetadata(IndustrialSystemData data)
	{
		if (data.Metadata == null || data.Metadata.Count == 0)
		{
			return (true, "No metadata to check");
		}

		var errors = new List<string>();
		
		if (data.Metadata.TryGetValue("maintenanceStatus", out var maintenanceStatus))
		{
			if (maintenanceStatus == "required" || maintenanceStatus == "overdue")
			{
				errors.Add($"Maintenance {maintenanceStatus}");
			}
		}

		if (data.Metadata.TryGetValue("lastServiceDate", out var lastServiceDate))
		{
			if (DateTime.TryParse(lastServiceDate, out var serviceDate))
			{
				var daysSinceService = (DateTime.Now - serviceDate).TotalDays;
				if (daysSinceService > 180)
				{
					errors.Add($"Last service too old: {daysSinceService:F0} days ago");
				}
			}
		}

		if (data.Metadata.TryGetValue("operatingMode", out var operatingMode))
		{
			if (operatingMode != "normal" && operatingMode != "optimal")
			{
				errors.Add($"System not in normal operating mode: {operatingMode}");
			}
		}

		return errors.Count > 0 
			? (false, string.Join("; ", errors)) 
			: (true, "Metadata checks passed");
	}
	
	private (bool success, string message) AnalyzeTrends(List<IndustrialSystemData> values)
    {
        if (values.Count < 3)
        {
            return (true, "Not enough data for trend analysis");
        }

        var errors = new List<string>();
        
        try
        {
	        var temperatureTrend = CalculateTemperatureTrend(values);
	        switch (temperatureTrend)
	        {
		        case > 5.0f:
			        errors.Add($"Rapidly increasing temperature trend: +{temperatureTrend:F1}°C/hour");
			        break;
		        case < -5.0f:
			        errors.Add($"Rapidly decreasing temperature trend: {temperatureTrend:F1}°C/hour");
			        break;
	        }
        }
        catch (Exception ex)
        {
            errors.Add($"Error analyzing temperature trend: {ex.Message}");
        }
        
        try
        {
            var pressureTrend = CalculatePressureTrend(values);
            if (Math.Abs(pressureTrend) > PRESSURE_CHANGE_THRESHOLD)
            {
                errors.Add($"Significant pressure change: {pressureTrend:F0} Pa/hour");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error analyzing pressure trend: {ex.Message}");
        }
        
        try
        {
            var cpuLoadTrend = CalculateCpuLoadTrend(values);
            if (cpuLoadTrend > 15.0f)
            {
                errors.Add($"Rapidly increasing CPU load trend: +{cpuLoadTrend:F1}%/hour");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error analyzing CPU load trend: {ex.Message}");
        }
        
        try
        {
            if (DetectTemperatureOscillations(values))
            {
                errors.Add("Abnormal temperature oscillations detected");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error detecting temperature oscillations: {ex.Message}");
        }

        return errors.Count > 0 
            ? (false, string.Join("; ", errors)) 
            : (true, "Trend analysis passed");
    }
	
	private float CalculateTemperatureTrend(List<IndustrialSystemData> values)
    {
        var tempValues = values
            .Where(v => v.Environment != null)
            .Select(v => new { Temp = v.Environment.Temperature, Time = v.Timestamp })
            .OrderBy(x => x.Time)
            .ToList();

        if (tempValues.Count < 2)
        {
	        return 0;
        }

        var firstReading = tempValues.First();
        var lastReading = tempValues.Last();
        
        var timeSpanHours = (lastReading.Time - firstReading.Time) / 3600.0;
        if (timeSpanHours < 0.01)
        {
	        return 0;
        }

        return (float)((lastReading.Temp - firstReading.Temp) / timeSpanHours);
    }

    private float CalculatePressureTrend(List<IndustrialSystemData> values)
    {
        var pressureValues = values
            .Where(v => v.Environment != null)
            .Select(v => new { v.Environment.Pressure, Time = v.Timestamp })
            .OrderBy(x => x.Time)
            .ToList();

        if (pressureValues.Count < 2)
        {
	        return 0;
        }

        var firstReading = pressureValues.First();
        var lastReading = pressureValues.Last();
        
        var timeSpanHours = (lastReading.Time - firstReading.Time) / 3600.0;
        if (timeSpanHours < 0.01)
        {
	        return 0;
        }

        return (float)((lastReading.Pressure - firstReading.Pressure) / timeSpanHours);
    }

    private float CalculateCpuLoadTrend(List<IndustrialSystemData> values)
    {
        var cpuValues = values
            .Where(v => v.SystemHealth != null)
            .Select(v => new { v.SystemHealth.CpuLoad, Time = v.Timestamp })
            .OrderBy(x => x.Time)
            .ToList();

        if (cpuValues.Count < 2)
        {
	        return 0;
        }

        var firstReading = cpuValues.First();
        var lastReading = cpuValues.Last();
        
        var timeSpanHours = (lastReading.Time - firstReading.Time) / 3600.0;
        if (timeSpanHours < 0.01)
        {
	        return 0;
        }

        return (float)((lastReading.CpuLoad - firstReading.CpuLoad) / timeSpanHours);
    }

    private bool DetectTemperatureOscillations(List<IndustrialSystemData> values)
    {
        if (values.Count < 6)
        {
	        return false;
        }

        var temperatures = values
            .Where(v => v.Environment != null)
            .Select(v => v.Environment.Temperature)
            .ToList();
        
        var directionChanges = 0;
        for (var i = 2; i < temperatures.Count; i++)
        {
            var prevDiff = temperatures[i - 1] - temperatures[i - 2];
            var currDiff = temperatures[i] - temperatures[i - 1];
            
            if (prevDiff * currDiff < 0)
            {
                directionChanges++;
            }
        }
        
        return directionChanges >= temperatures.Count / 3;
    }
    
    private (bool success, string message) DetectAnomalies(List<IndustrialSystemData> values)
    {
        if (values.Count < ANOMALY_DETECTION_WINDOW)
        {
            return (true, "Not enough data for anomaly detection");
        }

        var errors = new List<string>();

        try
        {
            var tempAnomalies = DetectTemperatureAnomalies(values);
            if (tempAnomalies.Count > 0)
            {
                errors.Add($"Detected {tempAnomalies.Count} temperature anomalies");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error detecting temperature anomalies: {ex.Message}");
        }

        try
        {
            var cpuLoadAnomalies = DetectCpuLoadAnomalies(values);
            if (cpuLoadAnomalies.Count > 0)
            {
                errors.Add($"Detected {cpuLoadAnomalies.Count} CPU load anomalies");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error detecting CPU load anomalies: {ex.Message}");
        }

        try
        {
            if (DetectDataQualityDrops(values))
            {
                errors.Add("Significant data quality drops detected");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error detecting data quality changes: {ex.Message}");
        }

        return errors.Count > 0 
            ? (false, string.Join("; ", errors)) 
            : (true, "Anomaly detection passed");
    }

    private List<int> DetectTemperatureAnomalies(List<IndustrialSystemData> values)
    {
        var anomalyIndices = new List<int>();
        var temperatures = values
            .Select((v, idx) => new { Temp = v.Environment?.Temperature ?? 0, Index = idx })
            .Where(x => x.Temp != 0)
            .ToList();

        if (temperatures.Count < ANOMALY_DETECTION_WINDOW)
        {
	        return anomalyIndices;
        }

        for (var i = ANOMALY_DETECTION_WINDOW; i < temperatures.Count; i++)
        {
            var window = temperatures.GetRange(i - ANOMALY_DETECTION_WINDOW, ANOMALY_DETECTION_WINDOW);
            var windowTemps = window.Select(x => x.Temp).ToList();
            
            var mean = windowTemps.Average();
            var stdDev = Math.Sqrt(windowTemps.Select(x => Math.Pow(x - mean, 2)).Sum() / windowTemps.Count);
            
            var current = temperatures[i];
            if (Math.Abs(current.Temp - mean) > 3 * stdDev)
            {
                anomalyIndices.Add(current.Index);
            }
        }

        return anomalyIndices;
    }
    
    private List<int> DetectCpuLoadAnomalies(List<IndustrialSystemData> values)
    {
	    var anomalyIndices = new List<int>();
	    var cpuLoads = values
		    .Select((v, idx) => new { Load = v.SystemHealth?.CpuLoad ?? 0, Index = idx })
		    .Where(x => x.Load != 0)
		    .ToList();

	    if (cpuLoads.Count < ANOMALY_DETECTION_WINDOW)
	    {
		    return anomalyIndices;
	    }

	    for (var i = ANOMALY_DETECTION_WINDOW; i < cpuLoads.Count; i++)
	    {
		    var window = cpuLoads.GetRange(i - ANOMALY_DETECTION_WINDOW, ANOMALY_DETECTION_WINDOW);
		    var windowLoads = window.Select(x => x.Load).ToList();
            
		    var mean = windowLoads.Average();
		    var stdDev = Math.Sqrt(windowLoads.Select(x => Math.Pow(x - mean, 2)).Sum() / windowLoads.Count);
            
		    var current = cpuLoads[i];
		    if (Math.Abs(current.Load - mean) > 2 * stdDev && current.Load > 70) 
		    {
			    anomalyIndices.Add(current.Index);
		    }
	    }

	    return anomalyIndices;
    }

    private bool DetectDataQualityDrops(List<IndustrialSystemData> values)
    {
	    var qualities = values
		    .Where(v => v.Environment != null)
		    .Select(v => (int)v.Environment.Quality)
		    .ToList();

	    if (qualities.Count < 5)
	    {
		    return false;
	    }

	    for (var i = 1; i < qualities.Count; i++)
	    {
		    if (qualities[i] - qualities[i-1] >= 2)
		    {
			    return true;
		    }
	    }

	    return false;
    }
    
    private (bool success, string message) PredictFailures(List<IndustrialSystemData> values)
    {
	    if (values.Count < 5)
	    {
		    return (true, "Not enough data for failure prediction");
	    }

	    var errors = new List<string>();

	    try
	    {
		    var systemHealthRisk = CalculateSystemHealthRisk(values);
		    switch (systemHealthRisk)
		    {
			    case > 0.8f:
				    errors.Add($"High risk of system failure: {systemHealthRisk:P0} risk score");
				    break;
			    case > 0.5f:
				    errors.Add($"Medium risk of system failure: {systemHealthRisk:P0} risk score");
				    break;
		    }
	    }
	    catch (Exception ex)
	    {
		    errors.Add($"Error predicting system failures: {ex.Message}");
	    }
	    try
	    {
		    if (DetectOverheatingRisk(values))
		    {
			    errors.Add("System shows signs of overheating risk");
		    }
	    }
	    catch (Exception ex)
	    {
		    errors.Add($"Error detecting overheating risk: {ex.Message}");
	    }

	    try
	    {
		    if (DetectOverloadRisk(values))
		    {
			    errors.Add("System shows signs of overload risk");
		    }
	    }
	    catch (Exception ex)
	    {
		    errors.Add($"Error detecting overload risk: {ex.Message}");
	    }

	    return errors.Count > 0 
		    ? (false, string.Join("; ", errors)) 
		    : (true, "No failure risks detected");
    }
    
    private float CalculateSystemHealthRisk(List<IndustrialSystemData> values)
    {
        var recentValues = values.Skip(Math.Max(0, values.Count - 5)).ToList();
       
        var temperatureRisk = recentValues
            .Where(v => v.Environment != null)
            .Select(v => Math.Min(Math.Max((v.Environment.Temperature - 50) / 35, 0), 1))
            .DefaultIfEmpty(0)
            .Average();

        var cpuRisk = recentValues
            .Where(v => v.SystemHealth != null)
            .Select(v => Math.Min(v.SystemHealth.CpuLoad / 100, 1))
            .DefaultIfEmpty(0)
            .Average();
        
        var batteryRisk = recentValues
            .Where(v => v.SystemHealth is { BatteryLevel: > 0 })
            .Select(v => Math.Min(Math.Max((25 - v.SystemHealth.BatteryLevel) / 25, 0), 1))
            .DefaultIfEmpty(0)
            .Average();
        
        var errorRisk = recentValues
            .Select(v => Math.Min(v.ErrorCount / 50.0f, 1))
            .DefaultIfEmpty(0)
            .Average();

        var dataQualityRisk = recentValues
            .Where(v => v.Environment != null)
            .Select(v => (float)v.Environment.Quality / 5)
            .DefaultIfEmpty(0)
            .Average();
        
        var alertRisk = recentValues
            .Where(v => v.Diagnostics is { Warnings: not null })
            .Select(v => {
                var criticalAlerts = v.Diagnostics.Warnings
                    .Count(w => w.Level is AlertLevel.Critical or AlertLevel.Emergency);
                return Math.Min(criticalAlerts / 3.0f, 1);
            })
            .DefaultIfEmpty(0)
            .Average();
        
        var totalRisk = temperatureRisk * 0.25f +
                        cpuRisk * 0.15f +
                        batteryRisk * 0.1f +
                        errorRisk * 0.2f +
                        dataQualityRisk * 0.1f +
                        alertRisk * 0.2f;

        return totalRisk;
    }
    
    private bool DetectOverheatingRisk(List<IndustrialSystemData> values)
    {
        var recentValues = values.OrderBy(v => v.Timestamp).ToList();
        if (recentValues.Count < 3)
        {
	        return false;
        }

        var temperatures = recentValues
            .Where(v => v.Environment != null)
            .Select(v => v.Environment.Temperature)
            .ToList();

        if (temperatures.Count < 3)
        {
	        return false;
        }

        var risingTemperature = true;
        for (var i = 1; i < temperatures.Count; i++)
        {
	        if (!(temperatures[i] <= temperatures[i - 1]))
	        {
		        continue;
	        }

	        risingTemperature = false;
	        break;
        }

        var highTemp = temperatures.LastOrDefault() > 70.0f;

        var cpuLoads = recentValues
            .Where(v => v.SystemHealth != null)
            .Select(v => v.SystemHealth.CpuLoad)
            .ToList();

        var highCpuLoad = cpuLoads.LastOrDefault() > 80.0f;
        
        return (risingTemperature && highTemp) || (highTemp && highCpuLoad);
    }

    private bool DetectOverloadRisk(List<IndustrialSystemData> values)
    {
        var recentValues = values.OrderBy(v => v.Timestamp).ToList();
        if (recentValues.Count < 3)
        {
	        return false;
        }

        var cpuLoads = recentValues
            .Where(v => v.SystemHealth != null)
            .Select(v => v.SystemHealth.CpuLoad)
            .ToList();

        if (cpuLoads.Count < 3)
        {
	        return false;
        }

        var risingCpuLoad = true;
        for (var i = 1; i < cpuLoads.Count; i++)
        {
	        if (!(cpuLoads[i] <= cpuLoads[i - 1]))
	        {
		        continue;
	        }

	        risingCpuLoad = false;
	        break;
        }

        var memoryUsages = recentValues
            .Where(v => v.SystemHealth != null)
            .Select(v => v.SystemHealth.MemoryUsage)
            .ToList();

        var risingMemoryUsage = true;
        for (var i = 1; i < memoryUsages.Count; i++)
        {
	        if (!(memoryUsages[i] <= memoryUsages[i - 1]))
	        {
		        continue;
	        }

	        risingMemoryUsage = false;
	        break;
        }

        var highCpuLoad = cpuLoads.LastOrDefault() > 85.0f;
        var highMemoryUsage = memoryUsages.LastOrDefault() > 90.0f;

        return (risingCpuLoad && highCpuLoad) || (risingMemoryUsage && highMemoryUsage);
    }

    private (bool success, string message) AnalyzeStatusPatterns(List<IndustrialSystemData> values)
    {
        if (values.Count < 5)
        {
            return (true, "Not enough data for status pattern analysis");
        }

        var errors = new List<string>();

        try
        {
            if (DetectStatusFlagPatterns(values))
            {
                errors.Add("Detected abnormal patterns in system status flags");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error analyzing status flag patterns: {ex.Message}");
        }

        try
        {
            if (DetectDataQualityPatterns(values))
            {
                errors.Add("Detected abnormal patterns in data quality");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error analyzing data quality patterns: {ex.Message}");
        }

        try
        {
            if (DetectErrorCountPatterns(values))
            {
                errors.Add("Detected abnormal error count patterns");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"Error analyzing error count patterns: {ex.Message}");
        }

        return errors.Count > 0 
            ? (false, string.Join("; ", errors)) 
            : (true, "Status pattern analysis passed");
    }

    private bool DetectStatusFlagPatterns(List<IndustrialSystemData> values)
    {
        var statuses = values
            .Where(v => v.SystemHealth is { StatusFlags: not null })
            .Select(v => v.SystemHealth.StatusFlags.GetValueOrDefault("systemState"))
            .Where(s => s != null)
            .ToList();

        if (statuses.Count < 5)
        {
	        return false;
        }

        var stateChanges = 0;
        for (var i = 1; i < statuses.Count; i++)
        {
            if (statuses[i] != statuses[i-1])
            {
                stateChanges++;
            }
        }

        return stateChanges > statuses.Count * 0.3;
    }

    private bool DetectDataQualityPatterns(List<IndustrialSystemData> values)
    {
        var qualities = values
            .Where(v => v.Environment != null)
            .Select(v => (int)v.Environment.Quality)
            .ToList();

        if (qualities.Count < 5)
        {
	        return false;
        }

        var consistentDegradation = true;
        for (var i = 1; i < qualities.Count; i++)
        {
	        if (qualities[i] >= qualities[i - 1])
	        {
		        continue;
	        }

	        consistentDegradation = false;
	        break;
        }

        if (consistentDegradation && qualities[^1] > qualities[0])
        {
            return true;
        }

        var qualityChanges = 0;
        for (var i = 1; i < qualities.Count; i++)
        {
            if (qualities[i] != qualities[i-1])
            {
                qualityChanges++;
            }
        }

        return qualityChanges > qualities.Count * 0.4;
    }

    private bool DetectErrorCountPatterns(List<IndustrialSystemData> values)
    {
        var errorCounts = values.Select(v => v.ErrorCount).ToList();

        if (errorCounts.Count < 5)
        {
	        return false;
        }

        var differences = new List<uint>();
        for (var i = 1; i < errorCounts.Count; i++)
        {
            differences.Add(errorCounts[i] - errorCounts[i-1]);
        }

        if (differences.Any(diff => diff > 10))
        {
	        return true;
        }

        var hasZero = errorCounts.Any(e => e == 0);
        var hasLarge = errorCounts.Any(e => e > 20);
        if (!hasZero || !hasLarge)
        {
	        return false;
        }

        {
	        var patternChanges = 0;
	        for (var i = 1; i < errorCounts.Count; i++)
	        {
		        var prev = errorCounts[i-1] < 5;
		        var curr = errorCounts[i] < 5;
		        if (prev != curr)
		        {
			        patternChanges++;
		        }
	        }

	        return patternChanges > errorCounts.Count * 0.3;
        }

    }
}
