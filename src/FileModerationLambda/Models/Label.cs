using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileModerationLambda.Models
{

    public record Label
    {
        public string Name { get; set; } = "";
        public float Confidence { get; set; }
    }
}
