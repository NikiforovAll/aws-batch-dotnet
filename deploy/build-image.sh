#!/bin/bash

cd "$(dirname "$0")"/../src

docker build -t aws-batch-dotnet-demo-repository .

# Login to Amazon ECR
# aws ecr-public get-login-password --region $AWS_REGION | docker login --username AWS --password-stdin public.ecr.aws

docker tag aws-batch-dotnet-demo-repository:latest public.ecr.aws/t7c5r3b7/aws-batch-dotnet-demo-repository:latest

docker push public.ecr.aws/t7c5r3b7/aws-batch-dotnet-demo-repository:latest

docker run -it --rm aws-batch-dotnet-demo-repository plan -h
