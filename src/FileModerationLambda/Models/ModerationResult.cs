using Amazon.Comprehend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileModerationLambda.Models
{
    public class ModerationResult
    {
        public string Bucket { get; set; } = "";
        public string Key { get; set; } = "";
        public string ContentType { get; set; } = "";
        public string? Language { get; set; }
        public List<Label>? ImageLabels { get; set; }
        public List<PiiEntity>? Pii { get; set; }
        public string? Note { get; set; }
        public string? Sentiment { get; set; }
        public SentimentScore? SentimentScores { get; set; }
    }
}
