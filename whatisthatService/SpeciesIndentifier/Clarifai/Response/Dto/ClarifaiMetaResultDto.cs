using System;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.SpeciesIndentifier.Clarifai.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class ClarifaiMetaResultDto
    {
        public ClarifaiTagMetaDto tag { get; set; }
    }
}