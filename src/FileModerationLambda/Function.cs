using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using static System.Net.Mime.MediaTypeNames;
using System;
using Image = Amazon.Rekognition.Model.Image;
using FileModerationLambda.Models;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaModeration;

public class Function
{
    private readonly IAmazonS3 _s3 = new AmazonS3Client();
    private readonly IAmazonRekognition _rekog = new AmazonRekognitionClient();
    private readonly IAmazonComprehend _comprehend = new AmazonComprehendClient();

    private readonly string _bucket = Env("BUCKET_NAME", required: true)!;
    private readonly string _approved = Env("APPROVED_PREFIX") ?? "approved/";
    private readonly string _quarantine = Env("QUARANTINE_PREFIX") ?? "quarantine/";
    private readonly string _reports = Env("REPORTS_PREFIX") ?? "moderation-reports/";
    private readonly float _minImageConf = float.Parse(Env("MIN_IMAGE_CONF") ?? "80"); // Rekognition expects %
    private readonly float _minPiiConf = float.Parse(Env("MIN_PII_CONF") ?? "80") / 100f; // Comprehend expects [0,1]

    public async Task FunctionHandler(S3Event evnt, ILambdaContext context)
    {
        foreach (var record in evnt.Records)
        {
            var key = record.S3.Object.Key;
            context.Logger.LogLine($"Moderating s3://{_bucket}/{key}");

            // Fetch metadata
            var head = await _s3.GetObjectMetadataAsync(_bucket, key);
            var contentType = head.Headers["Content-Type"] ?? "";
            var ext = Path.GetExtension(key).ToLowerInvariant();

            var result = new ModerationResult
            {
                Bucket = _bucket,
                Key = key,
                ContentType = contentType
            };

            bool flagged = false;

            // === IMAGE MODERATION ===
            if (IsImage(contentType, ext))
            {
                var imgRes = await _rekog.DetectModerationLabelsAsync(new DetectModerationLabelsRequest
                {
                    MinConfidence = _minImageConf,
                    Image = new Image
                    {
                        S3Object = new Amazon.Rekognition.Model.S3Object
                        {
                            Bucket = _bucket,
                            Name = key
                        }
                    }
                });

                result.ImageLabels = imgRes.ModerationLabels
                    .Select(l => new FileModerationLambda.Models.Label { Name = l.Name, Confidence = (float)l.Confidence })
                    .ToList();

                flagged = imgRes.ModerationLabels.Any(l => l.Confidence >= _minImageConf);
            }

            // === TEXT MODERATION ===
            else if (IsText(contentType, ext))
            {
                // Load file (max ~100KB)
                using var obj = await _s3.GetObjectAsync(_bucket, key);
                using var ms = new MemoryStream();
                await obj.ResponseStream.CopyToAsync(ms);
                var text = Encoding.UTF8.GetString(ms.ToArray(), 0, (int)Math.Min(ms.Length, 100_000));

                // Detect language
                var langRes = await _comprehend.DetectDominantLanguageAsync(
                    new DetectDominantLanguageRequest { Text = text });
                var langCode = langRes.Languages
                    .OrderByDescending(l => l.Score)
                    .FirstOrDefault()?.LanguageCode ?? "en";
                result.Language = langCode;

                // Detect PII
                var piiRes = await _comprehend.DetectPiiEntitiesAsync(
                    new DetectPiiEntitiesRequest { Text = text, LanguageCode = langCode });
                result.Pii = piiRes.Entities
                    .Select(e => new FileModerationLambda.Models.PiiEntity { Type = e.Type.Value, Score = e.Score })
                    .ToList();

                var hasPii = result.Pii.Any(e => e.Score >= _minPiiConf);

                // Sentiment
                var sentRes = await _comprehend.DetectSentimentAsync(
                    new DetectSentimentRequest { Text = text, LanguageCode = langCode });
                result.Sentiment = sentRes.Sentiment;
                result.SentimentScores = sentRes.SentimentScore;

                flagged = hasPii; // simple rule: quarantine if PII
            }

            // === UNSUPPORTED TYPES ===
            else
            {
                result.Note = "Skipped moderation (unsupported type)";
            }

            // === TAGGING + ROUTING ===
            var status = flagged ? "rejected" : "approved";

            await _s3.PutObjectTaggingAsync(new PutObjectTaggingRequest
            {
                BucketName = _bucket,
                Key = key,
                Tagging = new Tagging
                {
                    TagSet = new List<Amazon.S3.Model.Tag> { new Amazon.S3.Model.Tag { Key = "moderation", Value = status } }
                }
            });

            var destPrefix = flagged ? _quarantine : _approved;
            var destKey = destPrefix + key;

            await _s3.CopyObjectAsync(new CopyObjectRequest
            {
                SourceBucket = _bucket,
                SourceKey = key,
                DestinationBucket = _bucket,
                DestinationKey = destKey
            });

            // === WRITE REPORT ===
            var reportKey = $"{_reports}{key}.json";
            var reportJson = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });

            await _s3.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = reportKey,
                ContentBody = reportJson,
                ContentType = "application/json"
            });

            context.Logger.LogLine(
                $"Moderation complete: {status} | moved to {destKey} | report: {reportKey}"
            );
        }
    }

    private static bool IsImage(string ct, string ext) =>
        ct.StartsWith("image/") || new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(ext);

    private static bool IsText(string ct, string ext) =>
        ct.StartsWith("text/") || new[] { ".txt", ".md", ".json", ".csv" }.Contains(ext);

    private static string? Env(string key, bool required = false)
    {
        var v = Environment.GetEnvironmentVariable(key);
        if (required && string.IsNullOrEmpty(v))
            throw new InvalidOperationException($"Missing env var {key}");
        return v;
    }

  
}
