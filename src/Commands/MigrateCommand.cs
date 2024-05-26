namespace BatchMigration.Commands;

using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using BatchMigration.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

public class MigrateCommand(IAmazonS3 s3, IConfiguration configuration, ILogger<PlanCommand> logger)
    : CancellableAsyncCommand<MigrateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-p|--plan <PlanPath>")]
        public string Plan { get; set; } = default!;

        [CommandOption("-i|--index <Index>")]
        public int? Index { get; set; } = default!;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellation
    )
    {
        var plan = S3Path.Parse(settings.Plan);
        var index = settings.Index ?? configuration.GetValue<int>("JOB_ARRAY_INDEX");

        var migrationPlan = await this.GetPlanAsync(plan, cancellation);

        var file = migrationPlan!.Items[index];

        var fileSourcePath = new S3Path(
            migrationPlan.Metadata.Source.Bucket,
            Path.Combine(migrationPlan.Metadata.Source.Key, file)
        );
        var fileDestinationPath = new S3Path(
            migrationPlan.Metadata.Destination.Bucket,
            Path.Combine(migrationPlan.Metadata.Destination.Key, file)
        );

        var sourceText = await this.GetTextAsync(fileSourcePath, cancellation);

        var destinationText = CalculateWordsOccurrences(sourceText!);

        var stream = new MemoryStream(Encoding.UTF8.GetBytes(destinationText));

        await s3.PutObjectAsync(
            new PutObjectRequest()
            {
                BucketName = fileDestinationPath.Bucket,
                Key = fileDestinationPath.Key,
                InputStream = stream
            },
            cancellation
        );

        AnsiConsole.MarkupLine($"Plan: {plan}");
        AnsiConsole.MarkupLine($"Migrating file([blue]{index}[/]) - {fileSourcePath}");
        AnsiConsole.MarkupLine($"Migrating file([blue]{index}[/]) - {fileDestinationPath}");

        return 0;
    }

    private async Task<MigrationPlan?> GetPlanAsync(S3Path plan, CancellationToken cancellation)
    {
        using var response = await s3.GetObjectAsync(
            new() { BucketName = plan.Bucket, Key = plan.Key },
            cancellation
        );
        return await JsonSerializer.DeserializeAsync<MigrationPlan>(
            response.ResponseStream,
            cancellationToken: cancellation
        );
    }

    private async Task<string?> GetTextAsync(S3Path item, CancellationToken cancellation)
    {
        using var response = await s3.GetObjectAsync(
            new() { BucketName = item.Bucket, Key = item.Key },
            cancellation
        );
        using var reader = new StreamReader(response.ResponseStream);
        return await reader.ReadToEndAsync(cancellation);
    }

    public static string CalculateWordsOccurrences(string source)
    {
        var words = source.Split(' ').Select(ExtractWord);
        var dictionary = new Dictionary<string, int>();
        foreach (var word in words)
        {
            if (dictionary.TryGetValue(word, out var value))
            {
                dictionary[word] = ++value;
            }
            else
            {
                dictionary[word] = 1;
            }
        }
        return dictionary
            .OrderByDescending(kvp => kvp.Value)
            .Aggregate(
                new StringBuilder(),
                (sb, kvp) =>
                    sb.Append(kvp.Key).Append(':').Append(kvp.Value).Append('\n')
            )
            .ToString();
    }

    private static string ExtractWord(string x)
    {
#pragma warning disable CA1307 // Specify StringComparison for clarity
#pragma warning disable CA1308 // Normalize strings to uppercase

        var trimmed = x.Trim('.', ',', '!', '\n', '\t', '(', ')', '\r');
        trimmed = trimmed
            .Replace(".", "")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("\n", "")
            .Replace("\t", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("\r", "");
        return trimmed.ToLowerInvariant();

#pragma warning restore CA1308 // Normalize strings to uppercase
#pragma warning restore CA1307 // Specify StringComparison for clarity
    }
}
