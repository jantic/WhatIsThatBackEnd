using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.Core.Clarifai.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class ClarifaiTagResultDto
    {
        public List<String> classes { get; set; }
        public List<String> probs { get; set; }
    }
}