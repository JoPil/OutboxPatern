using System.Collections.Concurrent;
using System.Diagnostics;
using Dapper;
using MassTransit;
using Npgsql;
using System.Text.Json;


namespace OutboxProcessing.Outbox;


internal sealed class OutboxProcessor(
    NpgsqlDataSource dataSource,
    IPublishEndpoint publishEndpoint,
    ILogger<OutboxProcessor> logger)
{
    private const int BatchSize = 1000;
    private static readonly ConcurrentDictionary<string, Type> TypeCache = new();

    public async Task<int> Execute(CancellationToken cancellationToken = default)
    {
        var totalStopwatch = Stopwatch.StartNew();
        var stepStopwatch = new Stopwatch();

        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        stepStopwatch.Restart();
        var messages = (await connection.QueryAsync<OutboxMessage>(
            """
            SELECT id AS Id, type AS Type, content AS Content 
            FROM outbox_messages
            WHERE processed_on_utc IS NULL
            ORDER BY occurred_on_utc LIMIT @BatchSize
            """,
            new { BatchSize },
            transaction: transaction)).AsList();
        var queryTime = stepStopwatch.ElapsedMilliseconds;

        stepStopwatch.Restart();
        foreach (var message in messages)
        {
            try
            {
                var messageType = GetOrAddMessageType(message.Type);
                var deserializedMessage = JsonSerializer.Deserialize(message.Content, messageType)!;

                await publishEndpoint.Publish(deserializedMessage, messageType!, cancellationToken);

            }
            catch (Exception ex)
            {

            }
        }
        var publishTime = stepStopwatch.ElapsedMilliseconds;

        stepStopwatch.Restart();
        foreach (var message in messages)
        {
            try
            {
                await connection.ExecuteAsync(
                """
                UPDATE outbox_messages
                SET processed_on_utc = @ProcessedOnUtc
                WHERE id = @Id
                """,
                new { ProcessedOnUtc = DateTime.Now, message.Id },
                transaction: transaction);

            }
            catch (Exception ex)
            {
                await connection.ExecuteAsync(
                """
                UPDATE outbox_messages
                SET processed_on_utc = @ProcessedOnUtc, error = @Error
                WHERE id = @Id
                """,
                new { ProcessedOnUtc = DateTime.Now, Error = ex.ToString(), message.Id },
                transaction: transaction);
            }
        }
        var updateTime = stepStopwatch.ElapsedMilliseconds;

        await transaction.CommitAsync(cancellationToken);

        totalStopwatch.Stop();
        var totalTime = totalStopwatch.ElapsedMilliseconds;

        OutboxLoggers.LogProcessingPerformance(logger, totalTime, queryTime, publishTime, updateTime, messages.Count);

        return messages.Count;
    }

    private struct OutboxUpdate
    {
        public Guid Id { get; init; }
        public DateTime ProcessedOnUtc { get; init; }
        public string? Error { get; init; }
    }

    private static Type GetOrAddMessageType(string typeName)
    {
        return TypeCache.GetOrAdd(typeName, name => Messaging.Contracts.AssemblyReference.Assembly.GetType(name)!);
    }
}