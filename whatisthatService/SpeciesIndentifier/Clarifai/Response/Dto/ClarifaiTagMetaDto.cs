using System;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.SpeciesIndentifier.Clarifai.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class ClarifaiTagMetaDto
    {
        public String timestamp { get; set; }
        public String model { get; set; }
        public String config { get; set; }
    }
}