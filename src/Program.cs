using Amazon.S3;
using BatchMigration;
using BatchMigration.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

var services = ConfigureServices();

var app = new CommandApp(new TypeRegistrar(services));

app.Configure(config =>
{
    config
        .AddCommand<PlanCommand>("plan")
        .WithDescription("Prepares migration plan for a bucket")
        .WithExample(
            "plan",
            "--source s3://source-bucket",
            "--destination s3://destination-bucket/output",
            "--plan s3://destination-bucket/plan.json"
        );

    config
        .AddCommand<MigrateCommand>("migrate")
        .WithDescription("Run a migration based on migration plan and index")
        .WithExample("migrate", "--plan s3://destination-bucket/plan.json", "--index 1");

    config
        .AddCommand<MergeCommand>("merge")
        .WithDescription("Merge the results results")
        .WithExample("merge", "--source s3://destination-bucket/output");
});

var result = app.Run(args);

return result;

static ServiceCollection ConfigureServices()
{
    var services = new ServiceCollection();

    var configuration = new ConfigurationBuilder().AddEnvironmentVariables("AWS_BATCH_").Build();

    services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

    services.AddSingleton<IConfiguration>(configuration);

    services.AddSingleton<IAmazonS3, AmazonS3Client>();

    return services;
}
