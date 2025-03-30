using IoTServer;

namespace RuleEngine.Processor;

public class DummyValueProcessor : IDeviceValueProcessor
{
    private const int MinimumValue1 = 50;
    private const int MaximumValue1 = 5000;
    private const int WarningThresholdValue1 = 4500;
    private const int MinimumValue2 = 500;
    private const int MaximumValue2 = 20000;
    private const int CriticalRatioThreshold = 10;
    
    private readonly Queue<DeviceProducedValue> historicalData = new(10);
    private readonly object lockObject = new();
    
    public RuleEngineResult ProcessDeviceValue(DeviceProducedValue value)
    {
        var data = value.DummyValue;
        
        if (!data.ActiveStatus)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Error, 
                Message = "Device is not active.",
            };
        }
        
        UpdateHistoricalData(value);
        
        if (data.Value1 is < MinimumValue1 or > MaximumValue1)
        {
	        var threshold = data.Value1 < MinimumValue1 ? MinimumValue1 : MaximumValue1;
            return new RuleEngineResult { 
                EngineVerdict = Status.Error, 
                Message = $"Value1 out of range: {data.Value1}. " +
                          $"Valid range: {MinimumValue1}-{MaximumValue1}. Threshold: {threshold}",
            };
        }
        
        if (data.Value2 is < MinimumValue2 or > MaximumValue2)
        {
	        var threshold = data.Value1 < MinimumValue1 ? MinimumValue1 : MaximumValue1;
            return new RuleEngineResult { 
                EngineVerdict = Status.Error, 
                Message = $"Value2 out of range: {data.Value2}. " +
                          $"Valid range: {MinimumValue2}-{MaximumValue2}. Threshold: {threshold}"
            };
        }
        
        if (data.Value1 > 0 && data.Value2 / data.Value1 > CriticalRatioThreshold)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Warning, 
                Message = $"Critical value ratio detected: {data.Value2 / data.Value1}. " +
                          $"Max allowed: {CriticalRatioThreshold}. Ratio: { data.Value2 / data.Value1}"
            };
        }
        
        // Проверка контрольной суммы с использованием специального алгоритма
        var checksum = CalculateChecksum(data.Value1, data.Value2);
        if (checksum < 0 || checksum > 10000)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Error, 
                Message = $"Invalid checksum. Calculated checksum: {checksum}",
            };
        }
        
        lock (lockObject)
        {
	        if (historicalData.Count >= 3 && IsAnomaly(data))
	        {
		        return new RuleEngineResult { 
			        EngineVerdict = Status.Warning, 
			        Message = $"Anomaly detected in value pattern. AvgVal1: {GetAverageValue1()}, AvgVal2: {GetAverageValue2()}",
		        };
	        }
        }
        
        if (data.Value1 > WarningThresholdValue1)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Warning, 
                Message = $"Value1 approaching critical threshold: {data.Value1}/{MaximumValue1}.",
            };
        }
        
        return new RuleEngineResult { 
            EngineVerdict = Status.Ok, 
            Message = $"Device data is valid. Device efficiency: {CalculateEfficiency(data)}",
        };
    }

    public RuleEngineResult ProcessDeviceValues(IEnumerable<DeviceProducedValue> values)
    {
        var deviceValues = values.ToList();
        if (deviceValues.Count == 0)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Error, 
                Message = "No values provided for processing."
            };
        }
        
        var groupChecksum = 0;
        var activeDevices = 0;
        var maxValue1 = int.MinValue;
        var minValue1 = int.MaxValue;
        var errors = new List<string>();
        
        foreach (var data in deviceValues.Select(value => value.DummyValue))
        {
	        if (!data.ActiveStatus)
	        {
		        errors.Add("Device is not active.");
		        continue;
	        }
            
	        activeDevices++;
	        
	        maxValue1 = Math.Max(maxValue1, data.Value1);
	        minValue1 = Math.Min(minValue1, data.Value1);
            
	        groupChecksum = (groupChecksum + data.Value2 - data.Value1) * 17 % 100000;

	        if (data.Value1 > MaximumValue1 || data.Value2 > MaximumValue2)
	        {
		        errors.Add($"Device reports values out of range: V1={data.Value1}, V2={data.Value2}");
	        }
        }
        
        if (activeDevices < deviceValues.Count * 0.7)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Error, 
                Message = $"Too few active devices: {activeDevices}/{deviceValues.Count}"
            };
        }
        
        var value1Range = maxValue1 - minValue1;
        if (activeDevices > 1 && value1Range > MaximumValue1 * 0.5)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Ok, 
                Message = $"High variation in Value1 across devices: {value1Range}. Percent variation: {(double)value1Range / MaximumValue1 * 100}"
            };
        }
        
        if (groupChecksum is <= 1000 or > 90000)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Error, 
                Message = "Invalid group checksum.",
            };
        }
        
        if (errors.Count != 0)
        {
            return new RuleEngineResult { 
                EngineVerdict = Status.Ok, 
                Message = "Group data valid with warnings",
            };
        }
        
        return new RuleEngineResult { 
            EngineVerdict = Status.Ok, 
            Message = $"All device data is valid. GroupChecksum - {groupChecksum}",
        };
    }
    
    private int CalculateChecksum(int value1, int value2)
    {
        var checksum = 0;
        
        for (var i = 0; i < 16; i++)
        {
            var bit1 = (value1 >> i) & 1;
            var bit2 = (value2 >> i) & 1;
            checksum += bit1 * (i + 1) + bit2 * (17 - i);
        }
        
        return (checksum * 137 + value1 - value2) % 12000;
    }
    
    private void UpdateHistoricalData(DeviceProducedValue value)
    {
        lock (lockObject)
        {
            if (historicalData.Count >= 10)
            {
                historicalData.Dequeue();
            }
            historicalData.Enqueue(value);
        }
    }
    
    private bool IsAnomaly(DummyDeviceData currentData)
    {
        var avgValue1 = GetAverageValue1();
        var avgValue2 = GetAverageValue2();
        
        var deviation1 = Math.Abs(currentData.Value1 - avgValue1) / avgValue1;
        var deviation2 = Math.Abs(currentData.Value2 - avgValue2) / avgValue2;
        
        return deviation1 > 0.5 || deviation2 > 0.5;
    }
    
    private double GetAverageValue1()
    {
        lock (lockObject)
        {
            return historicalData.Count > 0 
                ? historicalData.Average(v => v.DummyValue.Value1) 
                : 0;
        }
    }
    
    private double GetAverageValue2()
    {
        lock (lockObject)
        {
            return historicalData.Count > 0 
                ? historicalData.Average(v => v.DummyValue.Value2) 
                : 0;
        }
    }
    
    private double CalculateEfficiency(DummyDeviceData data)
    {
        return Math.Min(100, (double)data.Value2 / data.Value1 * 50);
    }
}
