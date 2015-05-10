using System;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.Core.Clarifai.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class ClarifaiIndividualResultDto
    {
        public ClarifaiTagResultDto tag { get; set; }
    }
}