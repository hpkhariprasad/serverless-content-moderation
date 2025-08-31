![Build & Test](https://github.com/hpkhariprasad/serverless-file-uploader/actions/workflows/ci-cd.yml/badge.svg?branch=main&event=push&job=build-test)
![Terraform Validate](https://github.com/hpkhariprasad/serverless-file-uploader/actions/workflows/ci-cd.yml/badge.svg?branch=main&event=push&job=terraform-validate)
[![License](https://img.shields.io/badge/License-Apache%202.0-blue.svg)](./LICENSE)


# 🛡️ File Moderation Pipeline (AWS S3 + Lambda + Rekognition + Comprehend)

This project implements a **serverless content moderation system** using AWS services.  
It automatically scans uploaded files (text & images) in S3 and decides whether they should be **approved** or **quarantined**.

---

## 📌 Features
- **S3 Event Trigger** → New file uploads trigger the Lambda.
- **AWS Lambda (.NET 8)** → Handles moderation logic.
- **Amazon Comprehend** → Detects harmful/abusive text content.
- **Amazon Rekognition** → Detects unsafe/explicit/racy/violent images.
- **S3 Bucket Structure**
  - `uploads/` → Incoming files (trigger point)
  - `approved/` → Files passing moderation
  - `quarantine/` → Flagged files

---
## 🏗️ Architecture

```
               ┌─────────────────────────┐
               │        S3 Bucket        │
               │       (uploads/)        │
               └────────────┬────────────┘
                            │ (ObjectCreated event)
                            ▼
               ┌─────────────────────────┐
               │   AWS Lambda Function   │
               │  (FileModerationLambda) │
               └────────────┬────────────┘
          ┌─────────────────┴─────────────────┐
          │                                   │
          ▼                                   ▼
 ┌───────────────────┐              ┌───────────────────┐
 │ Amazon Comprehend │              │ Amazon Rekognition│
 │  (Text Analysis)  │              │ (Image Analysis)  │
 └─────────┬─────────┘              └─────────┬─────────┘
           │                                  │
           └──────────────┬───────────────────┘
                          ▼
               ┌─────────────────────────┐
               │   Moderation Decision   │
               │  (Approve / Quarantine) │
               └────────────┬────────────┘
                            │
          ┌─────────────────┴─────────────────┐
          ▼                                   ▼
 ┌───────────────────┐              ┌───────────────────┐
 │  S3 Bucket        │              │  S3 Bucket        │
 │  (approved/)      │              │  (quarantine/)    │
 └───────────────────┘              └───────────────────┘
```

## ⚙️ Deployment

### 1. Prerequisites
- [Terraform](https://developer.hashicorp.com/terraform/downloads) ≥ 1.5
- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- AWS CLI configured (`aws configure`)
- An AWS account with permissions for:
  - S3
  - Lambda
  - IAM
  - Comprehend
  - Rekognition

### 2. Build & Package Lambda
```sh
cd src/FileModerationLambda
dotnet restore
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained false -o ./publish
````

### 3. Deploy with Terraform

```sh
cd infrastructure
terraform init
terraform plan
terraform apply -auto-approve
```

This creates:

* `uploads` bucket (with notifications to Lambda)
* `approved` & `quarantine` buckets
* IAM roles & Lambda permissions

---

## 🧪 Testing

### 1. Upload Files

Upload a file into the `uploads/` bucket:

```sh
aws s3 cp ./samples/safe_text.txt s3://<your-bucket-name>/uploads/
```

### 2. Monitor Logs

Check Lambda execution logs:

```sh
aws logs tail /aws/lambda/FileModerationLambda --follow
```

### 3. Expected Behavior

* Safe content → moved to `approved/`
* Harmful/explicit content → moved to `quarantine/`

---

## 📂 Sample Test Cases

| Type  | Example Content / File | Expected Result   |
| ----- | ---------------------- | ----------------- |
| Text  | `"Hello world"`        | ✅ Approved        |
| Text  | `"I want to kill you"` | 🚫 Quarantine     |
| Image | Puppy / Landscape      | ✅ Approved        |
| Image | Explicit Nudity        | 🚫 Quarantine     |
| Image | Violence / Weapons     | 🚫 Quarantine     |
| Image | Suggestive Pose        | ⚠️ May be flagged |

You can place these test files in a local `samples/` folder and upload them to your bucket for validation.

---
## 📑 Sample Moderation Results

### 1. Safe Content
```json
{
  "Bucket": "content-moderation-uploads",
  "Key": "uploads/clean_text.txt",
  "ContentType": "text/plain",
  "Language": "en",
  "ImageLabels": null,
  "Pii": [],
  "Note": null,
  "Sentiment": "POSITIVE",
  "SentimentScores": {
    "Mixed": 7.6231234E-05,
    "Negative": 0.00024984582,
    "Neutral": 0.029916722,
    "Positive": 0.96975726
  }
}
```
### 2. PII Content

```json
{
  "Bucket": "content-moderation-uploads",
  "Key": "uploads/pii_text.txt",
  "ContentType": "text/plain",
  "Language": "en",
  "ImageLabels": null,
  "Pii": [
    {
      "Type": "NAME",
      "Score": 0.99989533
    },
    {
      "Type": "EMAIL",
      "Score": 0.9999112
    },
    {
      "Type": "PHONE",
      "Score": 0.9999054
    },
    {
      "Type": "CREDIT_DEBIT_NUMBER",
      "Score": 0.9999599
    }
  ],
  "Note": null,
  "Sentiment": "NEUTRAL",
  "SentimentScores": {
    "Mixed": 2.1122312E-06,
    "Negative": 5.1479095E-05,
    "Neutral": 0.9997993,
    "Positive": 0.00014709852
  }
}
```
### 3. Inappropriate Image

```json
{
  "Bucket": "content-moderation-uploads",
  "Key": "uploads/tumblr_80.jpg",
  "ContentType": "image/jpeg",
  "Language": null,
  "ImageLabels": [
    {
      "Name": "Swimwear or Underwear",
      "Confidence": 94.6311
    },
    {
      "Name": "Female Swimwear or Underwear",
      "Confidence": 94.6311
    },
    {
      "Name": "Non-Explicit Nudity of Intimate parts and Kissing",
      "Confidence": 91.4775
    },
    {
      "Name": "Non-Explicit Nudity",
      "Confidence": 91.4775
    }
  ],
  "Pii": null,
  "Note": null,
  "Sentiment": null,
  "SentimentScores": null
}
```

## 🧹 Cleanup

To destroy resources:

```sh
terraform destroy -auto-approve
```

## 🙌 Contributing

Pull requests are welcome! Please open an issue for feature requests or bug reports.

