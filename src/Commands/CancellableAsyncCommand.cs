namespace BatchMigration.Commands;

using Spectre.Console.Cli;

public abstract class CancellableAsyncCommand : AsyncCommand
{
    private readonly ConsoleAppCancellationTokenSource cancellationTokenSource = new();

    public abstract Task<int> ExecuteAsync(CommandContext context, CancellationToken cancellation);

    public sealed override async Task<int> ExecuteAsync(CommandContext context) =>
        await this.ExecuteAsync(context, this.cancellationTokenSource.Token);
}

public abstract class CancellableAsyncCommand<TSettings> : AsyncCommand<TSettings>
    where TSettings : CommandSettings
{
    private readonly ConsoleAppCancellationTokenSource cancellationTokenSource = new();

    public abstract Task<int> ExecuteAsync(
        CommandContext context,
        TSettings settings,
        CancellationToken cancellation
    );

    public sealed override async Task<int> ExecuteAsync(
        CommandContext context,
        TSettings settings
    ) => await this.ExecuteAsync(context, settings, this.cancellationTokenSource.Token);
}

#pragma warning disable CA1001 // Types that own disposable fields should be disposable
internal sealed class ConsoleAppCancellationTokenSource
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    private readonly CancellationTokenSource cts = new();

    public CancellationToken Token => this.cts.Token;

    public ConsoleAppCancellationTokenSource()
    {
        Console.CancelKeyPress += this.OnCancelKeyPress;
        AppDomain.CurrentDomain.ProcessExit += this.OnProcessExit;

        using var _ = this.cts.Token.Register(() =>
        {
            AppDomain.CurrentDomain.ProcessExit -= this.OnProcessExit;
            Console.CancelKeyPress -= this.OnCancelKeyPress;
        });
    }

    private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        // NOTE: cancel event, don't terminate the process
        e.Cancel = true;

        this.cts.Cancel();
    }

    private void OnProcessExit(object? sender, EventArgs e)
    {
        if (this.cts.IsCancellationRequested)
        {
            // NOTE: SIGINT (cancel key was pressed, this shouldn't ever actually hit however, as we remove the event handler upon cancellation of the `cancellationSource`)
            return;
        }

        this.cts.Cancel();
    }
}
