################################################################################
# Compute Environment(s)
################################################################################

output "compute_environments" {
  description = "Map of compute environments created and their associated attributes"
  value       = module.batch.compute_environments
}

################################################################################
# Compute Environment - Instance Role
################################################################################

output "instance_iam_role_name" {
  description = "The name of the IAM role"
  value       = module.batch.instance_iam_role_name
}

output "instance_iam_role_arn" {
  description = "The Amazon Resource Name (ARN) specifying the IAM role"
  value       = module.batch.instance_iam_role_arn
}

output "instance_iam_role_unique_id" {
  description = "Stable and unique string identifying the IAM role"
  value       = module.batch.instance_iam_role_unique_id
}

output "instance_iam_instance_profile_arn" {
  description = "ARN assigned by AWS to the instance profile"
  value       = module.batch.instance_iam_instance_profile_arn
}

output "instance_iam_instance_profile_id" {
  description = "Instance profile's ID"
  value       = module.batch.instance_iam_instance_profile_id
}

output "instance_iam_instance_profile_unique" {
  description = "Stable and unique string identifying the IAM instance profile"
  value       = module.batch.instance_iam_instance_profile_unique
}

################################################################################
# Compute Environment - Service Role
################################################################################

output "service_iam_role_name" {
  description = "The name of the IAM role"
  value       = module.batch.service_iam_role_name
}

output "service_iam_role_arn" {
  description = "The Amazon Resource Name (ARN) specifying the IAM role"
  value       = module.batch.service_iam_role_arn
}

output "service_iam_role_unique_id" {
  description = "Stable and unique string identifying the IAM role"
  value       = module.batch.service_iam_role_unique_id
}

################################################################################
# Compute Environment - Spot Fleet Role
################################################################################

output "spot_fleet_iam_role_name" {
  description = "The name of the IAM role"
  value       = module.batch.spot_fleet_iam_role_name
}

output "spot_fleet_iam_role_arn" {
  description = "The Amazon Resource Name (ARN) specifying the IAM role"
  value       = module.batch.spot_fleet_iam_role_arn
}

output "spot_fleet_iam_role_unique_id" {
  description = "Stable and unique string identifying the IAM role"
  value       = module.batch.spot_fleet_iam_role_unique_id
}

################################################################################
# Job Queue
################################################################################

output "job_queues" {
  description = "Map of job queues created and their associated attributes"
  value       = module.batch.job_queues
}

################################################################################
# Scheduling Policy
################################################################################

output "scheduling_policies" {
  description = "Map of scheduling policies created and their associated attributes"
  value       = module.batch.scheduling_policies
}

################################################################################
# Job Definitions
################################################################################

output "job_definitions" {
  description = "Map of job defintions created and their associated attributes"
  value       = module.batch.job_definitions
}

################################################################################
# Public Repository
################################################################################

output "public_repository_name" {
  description = "Name of the repository"
  value       = module.public_ecr.repository_name
}

output "public_repository_arn" {
  description = "Full ARN of the repository"
  value       = module.public_ecr.repository_arn
}

output "public_repository_registry_id" {
  description = "The registry ID where the repository was created"
  value       = module.public_ecr.repository_registry_id
}

output "public_repository_url" {
  description = "The URL of the repository (in the form `aws_account_id.dkr.ecr.region.amazonaws.com/repositoryName`)"
  value       = module.public_ecr.repository_url
}
