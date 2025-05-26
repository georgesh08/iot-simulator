import { check, sleep } from 'k6';
import grpc from 'k6/net/grpc';

const client = new grpc.Client();
client.load(['proto'], 'Device.proto');
client.load(['proto'], 'IoTDeviceService.proto');

export default () => {
  client.connect('controller:14620', { plaintext: true });

  // Вызов RegisterNewDevice
  const registerResponse = client.invoke('IoTServer.IoTDeviceService/RegisterNewDevice', {
    device: {
      name: 'Test Device 1',
      type: 0, // SENSOR
    },
  });

  check(registerResponse, {
    'register status is OK': (r) => r.status === 0,
    'deviceId received': (r) => r && r.message && r.message.deviceId !== undefined,
  });

  if (registerResponse.status !== 0) {
    console.error('RegisterNewDevice failed');
    client.close();
    return;
  }

  const deviceId = registerResponse.message.deviceId;

  // Вызов SendDeviceData с deviceId из регистрации
  const sendDataResponse = client.invoke('IoTServer.IoTDeviceService/SendDeviceData', {
    deviceId: deviceId,
    deviceValue: {
      industrialDeviceValue: {
        systemId: 'sys-001',
        systemName: 'Industrial System 1',
        environment: {
          temperature: 22.5,
          humidity: 45.0,
          pressure: 101325,
          lightLevel: 300,
          noiseLevel: 40,
          quality: 0, // EXCELLENT
        },
        tags: ['tag1', 'tag2'],
        errorCount: 0,
        metadata: { location: 'Factory Floor' },
        timestamp: Date.now(),
        systemHealth: {
          cpuLoad: 0.5,
          memoryUsage: 0.7,
          storageUsage: 0.3,
          batteryLevel: 0.9,
          uptime: 123456,
          activeProcesses: ['proc1', 'proc2'],
          statusFlags: { flag1: 'ok' },
        },
        diagnostics: {
          systemParams: { param1: 'value1' },
          logs: ['log1', 'log2'],
          warnings: [],
          rawDiagnosticDump: new Uint8Array([]),
          health: {
            cpuLoad: 0.5,
            memoryUsage: 0.7,
            storageUsage: 0.3,
            batteryLevel: 0.9,
            uptime: 123456,
            activeProcesses: ['proc1', 'proc2'],
            statusFlags: { flag1: 'ok' },
          },
        },
      },
    },
  });

  check(sendDataResponse, {
    'send data status is OK': (r) => r.status === 0,
  });

  client.close();
  sleep(1);
};
