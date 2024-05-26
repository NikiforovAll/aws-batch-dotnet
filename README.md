# AWS Batch Processing with .NET

The goal of this repository is to demonstrate how to use AWS Batch.

In this example we are processing files from S3 in parallel.

## Deploy resources

```bash
# cd ./deploy
terraform init
terraform apply
```

## Run Manually

Plan:

```bash
dotnet run -- plan \
    --source s3://aws-batch-demo-dotnet-source-bucket \
    --destination s3://aws-batch-demo-dotnet-destination-bucket/output/ \
    --plan s3://aws-batch-demo-dotnet-destination-bucket/plans/plan-01.json
```

Migrate:

```bash
numberOfJobs=2
for index in $(seq 0 $((numberOfJobs-1)))
do
    export AWS_BATCH_JOB_ARRAY_INDEX=$index
    dotnet run --no-build -- migrate \
        --plan s3://aws-batch-demo-dotnet-destination-bucket/plans/plan-01.json
done
```

Merge:

```bash
dotnet run -- merge \
    --source s3://aws-batch-demo-dotnet-destination-bucket/output/
```

## Submit Job in AWS batch

```bash
aws batch submit-job \
    --job-name aws-batch-dotnet-plan-01 \
    --job-queue MainQueue  \
    --job-definition aws-batch-dotnet-plan \
    --share-identifier "demobatch*" \
    --scheduling-priority-override 1 \
    --container-overrides '{
        "command": [
            "plan",
            "--source",
            "s3://aws-batch-demo-dotnet-source-bucket",
            "--destination",
            "s3://aws-batch-demo-dotnet-destination-bucket/output/",
            "--plan",
            "s3://aws-batch-demo-dotnet-destination-bucket/plans/plan-01.json"
        ]
    }'
```

```bash
aws batch submit-job \
    --job-name aws-batch-dotnet-migrate-01 \
    --job-queue MainQueue  \
    --job-definition aws-batch-dotnet-migrate \
    --share-identifier "demobatch*" \
    --scheduling-priority-override 1 \
    --array-properties size=2 \
    --container-overrides '{
        "command": [
            "migrate",
            "--plan",
            "s3://aws-batch-demo-dotnet-destination-bucket/plans/plan-01.json"
        ]
    }'
```

```bash
aws batch submit-job \
    --job-name aws-batch-dotnet-merge-01 \
    --job-queue MainQueue  \
    --job-definition aws-batch-dotnet-merge \
    --share-identifier "demobatch*" \
    --scheduling-priority-override 1 \
    --container-overrides '{
        "command": [
            "merge",
            "--source",
            "s3://aws-batch-demo-dotnet-destination-bucket/output/"
        ]
    }'
```

## Reference

* <https://docs.aws.amazon.com/batch/latest/userguide/example_array_job.html>
* <https://github.com/aws/aws-cli/issues/5636> - issue for ECR + Windows + SSO login via AWS CLI v2.
* <https://docs.aws.amazon.com/batch/latest/APIReference/API_SubmitJob.html>
* <https://docs.aws.amazon.com/step-functions/latest/dg/workflow-studio-process.html>
* <https://aws.amazon.com/blogs/hpc/encoding-workflow-dependencies-in-aws-batch/>
