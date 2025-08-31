using Xunit;
using Moq;
using Amazon.Lambda.S3Events;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Rekognition;
using Amazon.Rekognition.Model;
using Amazon.Comprehend;
using Amazon.Comprehend.Model;
using LambdaModeration;
using FileModerationLambda.Models;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;

namespace LambdaModeration.Tests
{
    public class FunctionTests
    {
        private LambdaModeration.Function CreateFunctionWithMocks(
            Mock<IAmazonS3> s3Mock,
            Mock<IAmazonRekognition> rekogMock,
            Mock<IAmazonComprehend> comprehendMock)
        {
            Environment.SetEnvironmentVariable("BUCKET_NAME", "test-bucket");
            Environment.SetEnvironmentVariable("APPROVED_PREFIX", "approved/");
            Environment.SetEnvironmentVariable("QUARANTINE_PREFIX", "quarantine/");
            Environment.SetEnvironmentVariable("REPORTS_PREFIX", "moderation-reports/");
            Environment.SetEnvironmentVariable("MIN_IMAGE_CONF", "80");
            Environment.SetEnvironmentVariable("MIN_PII_CONF", "80");
            var func = new LambdaModeration.Function();
            typeof(LambdaModeration.Function).GetField("_s3", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(func, s3Mock.Object);
            typeof(LambdaModeration.Function).GetField("_rekog", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(func, rekogMock.Object);
            typeof(LambdaModeration.Function).GetField("_comprehend", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(func, comprehendMock.Object);
            return func;
        }

        [Fact]
        public void IsImage_ReturnsTrueForImageContentTypeOrExtension()
        {
            var method = typeof(LambdaModeration.Function).GetMethod("IsImage", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            Assert.True((bool?)method.Invoke(null, new object[] { "image/jpeg", ".jpg" }) ?? throw new Exception("IsImage returned null"));
            Assert.True((bool?)method.Invoke(null, new object[] { "image/png", ".png" }) ?? throw new Exception("IsImage returned null"));
            Assert.True((bool?)method.Invoke(null, new object[] { "application/octet-stream", ".gif" }) ?? throw new Exception("IsImage returned null"));
            Assert.False((bool?)method.Invoke(null, new object[] { "text/plain", ".txt" }) ?? throw new Exception("IsImage returned null"));
        }

        [Fact]
        public void IsText_ReturnsTrueForTextContentTypeOrExtension()
        {
            var method = typeof(LambdaModeration.Function).GetMethod("IsText", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            Assert.True((bool?)method.Invoke(null, new object[] { "text/plain", ".txt" }) ?? throw new Exception("IsText returned null"));
            Assert.True((bool?)method.Invoke(null, new object[] { "text/csv", ".csv" }) ?? throw new Exception("IsText returned null"));
            Assert.True((bool?)method.Invoke(null, new object[] { "application/json", ".json" }) ?? throw new Exception("IsText returned null"));
            Assert.False((bool?)method.Invoke(null, new object[] { "image/jpeg", ".jpg" }) ?? throw new Exception("IsText returned null"));
        }

        [Fact]
        public void Env_ReturnsValueOrThrowsIfRequired()
        {
            var method = typeof(LambdaModeration.Function).GetMethod("Env", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);
            Environment.SetEnvironmentVariable("TEST_ENV", "value");
            Assert.Equal("value", method.Invoke(null, new object[] { "TEST_ENV", false }) as string);
            Environment.SetEnvironmentVariable("TEST_ENV", null);
            Assert.Null(method.Invoke(null, new object[] { "TEST_ENV", false }));
            Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { "TEST_ENV", true }));
        }
    }
}
