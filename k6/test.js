import { check, sleep } from 'k6';
import grpc from 'k6/net/grpc';

function generateRandomDeviceName() {
  const prefix = 'Device';
  const randomId = Math.floor(Math.random() * 1000000);
  return `${prefix}-${randomId}`;
}

// Функция для генерации случайных данных устройства
function generateDeviceData(deviceId) {
  return {
    deviceId: deviceId,
    deviceValue: {
      industrialDeviceValue: {
        systemId: "System-" + Math.floor(Math.random() * 1000),
        systemName: "Industrial-Sensor",
        environment: {
          temperature: Math.random() * 50 - 10, // от -10 до +40
          humidity: Math.random() * 100,       // от 0 до 100
          pressure: 900 + Math.random() * 200, // от 900 до 1100
          lightLevel: Math.random() * 1000,    // от 0 до 1000
          noiseLevel: Math.random() * 100,     // от 0 до 100
          quality: Math.floor(Math.random() * 3) // 0-2 (NONE, GOOD, BAD)
        },
        tags: ["sensor", "industrial", "monitoring"],
        errorCount: Math.floor(Math.random() * 10),
        metadata: {
          "location": "factory-floor",
          "manufacturer": "IndustrialIoT Inc."
        },
        timestamp: Date.now().toString(),
        systemHealth: {
          cpuLoad: Math.random() * 100,
          memoryUsage: Math.random() * 100,
          storageUsage: Math.random() * 100,
          batteryLevel: Math.random() * 100,
          uptime: (3600 + Math.floor(Math.random() * 86400)).toString(), // 1-24 hours
          activeProcesses: ["data-collector", "networking"]
        },
        diagnostics: {
          logs: ["System operational", "Data collection active"],
          warnings: [
            {
              alertId: "temp-alert",
              level: Math.random() > 0.8 ? 3 : 2, // 20% chance for CRITICAL
              source: "temperature-sensor",
              timestamp: Date.now().toString(),
              acknowledged: false
            }
          ]
        }
      }
    }
  };
}

const CONTAINER_NAME = __ENV.TEST_NAME|| "k6_test";

import { htmlReport } from "./k6-libs/bundle.js";
import { textSummary } from "./k6-libs/index.js";

export function handleSummary(data) {

  const reportPrefix = `${CONTAINER_NAME}_summary`;

  return {
    [`/tmp/output/${reportPrefix}.html`]: htmlReport(data),
    [`/tmp/output/${reportPrefix}.json`]: JSON.stringify(data),
    stdout: textSummary(data, { indent: " ", enableColors: true }),
  };
}

const VUS = parseInt(__ENV.VUS) || 1;
const RATE = __ENV.RATE || 1;
const DURATION = __ENV.DURATION || '1m';

export const options = {
  vus: VUS, // Количество виртуальных пользователей
  duration: DURATION, // Длительность теста
};

const client = new grpc.Client();
client.load(['proto'], 'Device.proto');
client.load(['proto'], 'IoTDeviceService.proto');

export default () => {
  client.connect('controller:18686', { plaintext: true });

  // 1. Регистрация нового устройства
  const registerResponse = client.invoke('IoTServer.IoTDeviceService/RegisterNewDevice', {
    device: {
      name: generateRandomDeviceName(),
      type: 0, // SENSOR
    },
  });

  check(registerResponse, {
    'register status is OK': (r) => r && r.message && r.message.status === "StatusOk",
    'deviceId received': (r) => r && r.message && r.message.deviceId !== undefined,
  });

  if (!registerResponse.message || registerResponse.message.status !== "StatusOk") {
    console.error('RegisterNewDevice failed');
    client.close();
    return;
  }

  const deviceId = registerResponse.message.deviceId;

  // 2. Отправка 3 сообщений с данными
  for (let i = 0; i < 3; i++) {
    const data = generateDeviceData(deviceId);
    const sendDataResponse = client.invoke('IoTServer.IoTDeviceService/SendDeviceData', data);
    
    check(sendDataResponse, {
      [`send data ${i+1} status is OK`]: (r) => r && r.message && r.message.status === "StatusOk",
    });

    if (!sendDataResponse.message || sendDataResponse.message.status !== "StatusOk") {
      console.error(`SendDeviceData ${i+1} failed`);
    } else {
      //console.log(`Message ${i+1} sent successfully`);
    }

    sleep(RATE); 
  }

  client.close();
  sleep(1);
};