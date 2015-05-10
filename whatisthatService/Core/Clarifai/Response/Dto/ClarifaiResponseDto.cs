using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.Core.Clarifai.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class ClarifaiResponseDto
    {
        public String status_code { get; set; }
        public String status_msg { get; set; }
        public ClarifaiMetaResultDto meta { get; set; }
        public List<ClarifaiMultiResultDto> results { get; set; }
    }
}