variable "aws_region" {
  description = "AWS region"
  type        = string
  default     = "ap-south-1" # Mumbai
}

variable "bucket_name_prefix" {
  description = "Prefix for S3 bucket name (account ID will be appended)"
  type        = string
  default     = "content-moderation-uploads"
}

variable "lambda_function_name" {
  description = "Moderator Lambda function name"
  type        = string
  default     = "file-uploader-moderator"
}

variable "approved_prefix" {
  description = "Prefix for approved (safe) objects"
  type        = string
  default     = "approved/"
}

variable "quarantine_prefix" {
  description = "Prefix for quarantined (flagged) objects"
  type        = string
  default     = "quarantine/"
}

variable "reports_prefix" {
  description = "Prefix for JSON moderation reports"
  type        = string
  default     = "moderation-reports/"
}

variable "rekognition_min_confidence" {
  description = "Rekognition moderation min confidence (0-100)"
  type        = number
  default     = 80
}

variable "comprehend_min_pii_confidence" {
  description = "Comprehend PII min confidence (0-100)"
  type        = number
  default     = 80
}

variable "lambda_memory_mb" {
  description = "Lambda memory size"
  type        = number
  default     = 512
}

variable "lambda_timeout_seconds" {
  description = "Lambda timeout in seconds"
  type        = number
  default     = 60
}
