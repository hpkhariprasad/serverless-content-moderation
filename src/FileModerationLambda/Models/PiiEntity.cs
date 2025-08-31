using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileModerationLambda.Models
{
    public record PiiEntity
    {
        public string Type { get; set; } = "";
        public float? Score { get; set; }
    }
}
