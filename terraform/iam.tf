resource "aws_iam_role" "moderator_role" {
  name = "lambda-file-moderator-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17",
    Statement = [{
      Effect = "Allow",
      Principal = { Service = "lambda.amazonaws.com" },
      Action = "sts:AssumeRole"
    }]
  })
}

# CloudWatch Logs
resource "aws_iam_role_policy_attachment" "moderator_logs" {
  role       = aws_iam_role.moderator_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AWSLambdaBasicExecutionRole"
}

# S3 + Comprehend + Rekognition permissions
resource "aws_iam_role_policy" "moderator_policy" {
  name = "lambda-file-moderator-policy"
  role = aws_iam_role.moderator_role.id

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect: "Allow",
        Action: [
          "s3:GetObject",
          "s3:GetObjectTagging",
          "s3:PutObjectTagging",
          "s3:PutObject",
          "s3:CopyObject",
          "s3:ListBucket"
        ],
        Resource: [
          aws_s3_bucket.uploads.arn,
          "${aws_s3_bucket.uploads.arn}/*"
        ]
      },
      {
        Effect: "Allow",
        Action: [
          "comprehend:DetectPiiEntities",
          "comprehend:DetectSentiment",
          "comprehend:DetectDominantLanguage"
        ],
        Resource: "*"
      },
      {
        Effect: "Allow",
        Action: [
          "rekognition:DetectModerationLabels"
        ],
        Resource: "*"
      }
    ]
  })
}
