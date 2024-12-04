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

Optimize Db queries by using indexes.

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

### 4th experiment

Create cache to avoid using reflection every time to get the type of message and use batch publishing messages to rabbitmq

```
    Outbox processing completed.
    - Total time: 797ms
    - Query time: 2ms
    - Publish time: 373ms
    - Update time: 396ms
    - Messages processed: 1000

    OutboxBackgroundService finished.
    - Total iterations: 75
    - Total processed messages: 74000
```

### 5th experiment

Optimizing UPDATE queries

```
    Outbox processing completed.
    - Total time: 481ms
    - Query time: 7ms
    - Publish time: 409ms
    - Update time: 61ms
    - Messages processed: 1000

    OutboxBackgroundService finished.
    - Total iterations: 124
    - Total processed messages: 123000
```

### 6th experiment

Using multiple parallel outbox processors

```
    Outbox processing completed.
    - Total time: 68ms
    - Query time: 8ms
    - Publish time: 39ms
    - Update time: 16ms
    - Messages processed: 1000

    OutboxBackgroundService finished.
    - Total iterations: 1957
    - Total processed messages: 1953000
```

MAX 32,550 Mps
