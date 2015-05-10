using System;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.SpeciesIndentifier.Wolfram.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class WolframResponseDto
    {
        public String Success { set; get; }
        public String StatusCode { set; get; }
        public String InputString { set; get; }
        public String[] MessagesText { set; get; }
        public String Timing { set; get; }
        public String AbsoluteTiming { set; get; }
        public String Result { set; get; }
        public String FailureType { set; get; }
        public String[] Messages { set; get; }
        public String[] MessagesExpressions { get; set; }
    }
}