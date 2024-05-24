namespace BatchMigration.Commands;

using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using BatchMigration.Models;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

public class MergeCommand(IAmazonS3 s3, ILogger<MergeCommand> logger)
    : CancellableAsyncCommand<MergeCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-s|--source <SourcePath>")]
        public string Source { get; set; } = default!;
    }

    public override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellation
    )
    {
        ArgumentNullException.ThrowIfNull(settings.Source);

        var sourcePath = S3Path.Parse(settings.Source);

        var files = await this.GetFilesAsync(sourcePath.Bucket, sourcePath.Key, cancellation);

        var fileLines = new ConcurrentDictionary<string, int>();

        await Task.WhenAll(
            files.Select(async f =>
            {
                var content = await s3.GetObjectAsync(
                    new GetObjectRequest { BucketName = sourcePath.Bucket, Key = f },
                    cancellation
                );

                var contentString = await new StreamReader(content.ResponseStream).ReadToEndAsync();
                var lines = contentString.Split(Environment.NewLine);
                Parallel.ForEach(
                    lines,
                    line =>
                    {
                        var parts = line.Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1], out var value))
                        {
                            fileLines.AddOrUpdate(
                                parts[0],
                                value,
                                (_, existingValue) => existingValue + value
                            );
                        }
                    }
                );
            })
        );

        var top = fileLines
            .Where(x =>
                x.Key
                    is { Length: > 2 }
                        and not "and"
                        and not "the"
            )
            .Where(x => x.Value >= 3)
            .OrderByDescending(x => x.Value)
            .Take(100);

        WriteTable(top);

        return 0;
    }

    private static void WriteTable(IEnumerable<KeyValuePair<string, int>> items)
    {
        var table = new Table();
        table.AddColumn("Key");
        table.AddColumn(new TableColumn("Value").Centered());

        foreach (var item in items)
        {
            table.AddRow(item.Key, item.Value.ToString(CultureInfo.InvariantCulture));
        }

        AnsiConsole.Write(table);
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
}
