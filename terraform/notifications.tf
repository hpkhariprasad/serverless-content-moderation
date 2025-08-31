# Allow S3 to invoke the Lambda
resource "aws_lambda_permission" "allow_s3_invoke_moderator" {
  statement_id  = "AllowExecutionFromS3"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.moderator.arn
  principal     = "s3.amazonaws.com"
  source_arn    = aws_s3_bucket.uploads.arn
}

# S3 -> Lambda on new object (only when file is placed in uploads/ folder)
resource "aws_s3_bucket_notification" "uploads_notifications" {
  bucket = aws_s3_bucket.uploads.id

  lambda_function {
    lambda_function_arn = aws_lambda_function.moderator.arn
    events              = ["s3:ObjectCreated:*"]

    # Avoid infinite loops by triggering only on incoming uploads
    filter_prefix = "uploads/"
  }

  depends_on = [aws_lambda_permission.allow_s3_invoke_moderator]
}
