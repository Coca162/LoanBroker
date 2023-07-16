namespace LoanBroker.Workers;

public class DepositWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    public readonly ILogger<DepositWorker> _logger;

    public DepositWorker(ILogger<DepositWorker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Task task = Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        using var _dbctx = BrokerContext.DbFactory.CreateDbContext();

                        if (DateTime.UtcNow.Subtract(DBCache.TimeInfo.LastLoanUpdate).TotalHours >= 1)
                        {
                            await AccountSystem.DoHourlyTick(_dbctx);
                            DBCache.TimeInfo.LastLoanUpdate = DateTime.UtcNow;
                            await _dbctx.SaveChangesAsync();
                        }

                        await AccountSystem.UpdateDeposits(_dbctx);

                        await _dbctx.SaveChangesAsync();

                        //await Task.Delay(1000 * 60 * 60);
                        await Task.Delay(1000 * 60 * 1);
                    }
                    catch (System.Exception e)
                    {
                        Console.WriteLine("FATAL ECONOMY WORKER ERROR:");
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.StackTrace);
                        if (e.InnerException is not null)
                            Console.WriteLine(e.InnerException);
                    }
                }
            });

            while (!task.IsCompleted)
            {
                await DBCache.SaveAsync();
                await Task.Delay(10_000, stoppingToken);
            }

            _logger.LogInformation("Economy Worker task stopped at: {time}", DateTimeOffset.Now);
            _logger.LogInformation("Restarting.", DateTimeOffset.Now);
        }
    }
}