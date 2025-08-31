output "bucket_name" {
  description = "S3 bucket for uploads"
  value       = aws_s3_bucket.uploads.bucket
}

output "moderator_lambda_name" {
  description = "Moderator Lambda function name"
  value       = aws_lambda_function.moderator.function_name
}
