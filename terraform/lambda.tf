resource "aws_lambda_function" "moderator" {
  function_name = var.lambda_function_name
  role          = aws_iam_role.moderator_role.arn
  handler       = "FileModerationLambda::LambdaModeration.Function::FunctionHandler"

  runtime       = "dotnet8"

  filename         = "../package/FileModerationLambda.zip"              # <- place zip here
  source_code_hash = filebase64sha256("../package/FileModerationLambda.zip")

  memory_size = var.lambda_memory_mb
  timeout     = var.lambda_timeout_seconds

  environment {
    variables = {
      BUCKET_NAME       = aws_s3_bucket.uploads.bucket
      APPROVED_PREFIX   = var.approved_prefix
      QUARANTINE_PREFIX = var.quarantine_prefix
      REPORTS_PREFIX    = var.reports_prefix
      MIN_IMAGE_CONF    = tostring(var.rekognition_min_confidence)
      MIN_PII_CONF      = tostring(var.comprehend_min_pii_confidence)
    }
  }

  depends_on = [aws_s3_bucket.uploads]
}
