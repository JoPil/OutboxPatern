# Optimizing outbox pattern

## Initial container setup

You will need a rabbitmq instance and a postgres database. Remember to run it on "release" mode.

```
docker compose up

dotnet run --property:Configuration=Release
```

### 1st experiment

Using BatchSize = 10 on OutboxProcessor

```
    Outbox processing completed.
    - Total time: 75ms
    - Query time: 64ms
    - Publish time: 5ms
    - Update time: 4ms
    Messages processed: 10

    OutboxBackgroundService finished.
    - Total iterations: 761
    - Total processed messages: 7600
```

### 2nd experiment

Instead of processing 10 messages at a time, BatchSize is increased to 1000.

```
    Outbox processing completed.
    - Total time: 857ms
    - Query time: 65ms
    - Publish time: 406ms
    - Update time: 384ms
    - Messages processed: 1000

    OutboxBackgroundService finished.
    - Total iterations: 68
    - Total processed messages: 67000
```

### 3rd experiment

Instead of processing 10 messages at a time, BatchSize is increased to 1000.

```
    Outbox processing completed.
    - Total time: 800ms
    - Query time: 3ms
    - Publish time: 419ms
    - Update time: 375ms
    - Messages processed: 1000

    OutboxBackgroundService finished.
    - Total iterations: 72
    - Total processed messages: 71000
```
