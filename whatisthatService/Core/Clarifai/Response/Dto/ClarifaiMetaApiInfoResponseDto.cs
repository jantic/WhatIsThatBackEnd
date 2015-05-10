using System;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.Core.Clarifai.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class ClarifaiMetaApiInfoResponseDto
    {
        public String status_code { get; set; }
        public String status_msg { get; set; }
        public ClarifaiApiInfoResultDto results { get; set; }
    }
}