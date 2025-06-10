set -e

echo "Ждем запуска контейнеров..."
sleep 60


echo "Инициализируем config server replica set"
mongosh --host configsvr1 --port 27019 <<EOF
rs.initiate({
  _id: "configReplSet",
  configsvr: true,
  members: [
    { _id: 0, host: "configsvr1:27019" },
    { _id: 1, host: "configsvr2:27019" },
    { _id: 2, host: "configsvr3:27019" }
  ]
});
EOF

echo "Ждем replica"
sleep 60

echo "Инициализируем shard1 replica set"
mongosh --host shard1_1 --port 27018 <<EOF
rs.initiate({
  _id: "shard1ReplSet",
  members: [
    { _id: 0, host: "shard1_1:27018" },
    { _id: 1, host: "shard1_2:27018" },
    { _id: 2, host: "shard1_3:27018" }
  ]
});
EOF

echo "Инициализируем shard2 replica set"
mongosh --host shard2_1 --port 27018 <<EOF
rs.initiate({
  _id: "shard2ReplSet",
  members: [
    { _id: 0, host: "shard2_1:27018" },
    { _id: 1, host: "shard2_2:27018" },
    { _id: 2, host: "shard2_3:27018" }
  ]
});
EOF

echo "Добавляем шарды в кластер через mongos"
mongosh --host mongos --port 27017 <<EOF
sh.addShard("shard1ReplSet/shard1_1:27018,shard1_2:27018,shard1_3:27018");
sh.addShard("shard2ReplSet/shard2_1:27018,shard2_2:27018,shard2_3:27018");
sh.status();
print("Enable DB sharding...");
sh.enableSharding("DevicesDb");

print("Enable collection sharding...");
sh.shardCollection("DevicesDb.DeviceData", { DeviceId: "hashed" });
sh.shardCollection("DevicesDb.Devices", { _id: "hashed" });

print("Кластер MongoDB с репликацией и шардированием успешно инициализирован!");
EOF


