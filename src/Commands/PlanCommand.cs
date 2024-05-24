namespace BatchMigration.Commands;

using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using BatchMigration.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

public class PlanCommand(IAmazonS3 s3, ILogger<PlanCommand> logger)
    : CancellableAsyncCommand<PlanCommand.Settings>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new() { WriteIndented = true };

    public class Settings : CommandSettings
    {
        [CommandOption("-s|--source <SourcePath>")]
        public string Source { get; set; } = default!;

        [CommandOption("-d|--destination <DestinationPath>")]
        public string Destination { get; set; } = default!;

        [CommandOption("-p|--plan <PlanPath>")]
        public string Plan { get; set; } = default!;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellation
    )
    {
        ArgumentNullException.ThrowIfNull(settings.Source);
        ArgumentNullException.ThrowIfNull(settings.Destination);
        ArgumentNullException.ThrowIfNull(settings.Plan);

        var (source, destination, plan) = (
            S3Path.Parse(settings.Source),
            S3Path.Parse(settings.Destination),
            S3Path.Parse(settings.Plan)
        );

        await AnsiConsole
            .Status()
            .AutoRefresh(true)
            .StartAsync(
                "Preparing plan...",
                async ctx =>
                {
                    var files = await this.GetFilesAsync(source.Bucket, source.Key, cancellation);

                    var items = files
                        .Select(file =>
                            !string.IsNullOrWhiteSpace(source.Key)
                                ? file.Replace(source.Key, string.Empty, StringComparison.Ordinal)
                                : file
                        )
                        .ToList();

                    var migrationPlan = new MigrationPlan(
                        new(source, destination, plan, items.Count),
                        items
                    );

                    using var stream = new MemoryStream();
                    await JsonSerializer.SerializeAsync(
                        stream,
                        migrationPlan,
                        JsonSerializerOptions
                    );
                    stream.Position = 0;

                    await this.PutObjectAsync(plan.Bucket, plan.Key, stream, cancellation);
                }
            );

        AnsiConsole.MarkupLine($"Running scanning for {source}");

        logger.LogDebug("Saved plan at {PlanPath}", plan);

        AnsiConsole.MarkupLine($"Result of the scan will be saved to {destination}");
        AnsiConsole.MarkupLine($"Plan can be found here {plan}");

        return 0;
    }

    private async Task<IEnumerable<string>> GetFilesAsync(
        string source,
        string? path,
        CancellationToken cancellationToken = default
    )
    {
        List<string> fileList = [];
        var listObjectsV2Paginator = s3.Paginators.ListObjectsV2(
            new ListObjectsV2Request { BucketName = source, Prefix = path, }
        );

        await foreach (
            var response in listObjectsV2Paginator.Responses.WithCancellation(cancellationToken)
        )
        {
            fileList.AddRange(
                response.S3Objects.Where(x => !x.Key.EndsWith('/')).Select(x => x.Key)
            );
        }

        return fileList;
    }

    private async Task PutObjectAsync(
        string source,
        string? path,
        Stream stream,
        CancellationToken cancellationToken = default
    )
    {
        var putObjectRequest = new PutObjectRequest
        {
            BucketName = source,
            Key = path,
            InputStream = stream
        };

        await s3.PutObjectAsync(putObjectRequest, cancellationToken);
    }
}

internal sealed record MigrationPlan(Metadata Metadata, IList<string> Items) { }

internal sealed record Metadata(S3Path Source, S3Path Destination, S3Path Plan, int TotalItems);
