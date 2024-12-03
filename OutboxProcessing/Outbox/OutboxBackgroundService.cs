using OutboxProcessing.Outbox;

namespace OutboxProcessing.Outbox;

internal class OutboxBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    ILogger<OutboxBackgroundService> logger) : BackgroundService
{

    private const int OutboxProcessorFrequency = 5;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        OutboxLoggers.LogStarting(logger);

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, stoppingToken);

        int totalProcessedMessages = 0;
        int iterationCount = 0;

        try
        {
            using var scope = serviceScopeFactory.CreateScope();
            var outboxProcessor = scope.ServiceProvider.GetRequiredService<OutboxProcessor>();

            while (!linkedCts.IsCancellationRequested)
            {
                iterationCount++;
                OutboxLoggers.LogStartingIteration(logger, iterationCount);

                int processedMessages = await outboxProcessor.Execute(linkedCts.Token);
                totalProcessedMessages += processedMessages;

                OutboxLoggers.LogIterationCompleted(logger, iterationCount, processedMessages, totalProcessedMessages);

                // Simulate running Outbox processing every N seconds
                //await Task.Delay(TimeSpan.FromSeconds(OutboxProcessorFrequency), linkedCts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            OutboxLoggers.LogOperationCancelled(logger);
        }
        catch (Exception ex)
        {
            OutboxLoggers.LogError(logger, ex);
        }
        finally
        {
            OutboxLoggers.LogFinished(logger, iterationCount, totalProcessedMessages);
        }
    }
}
