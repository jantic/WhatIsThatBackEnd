using System;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.SpeciesIndentifier.Clarifai.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class ClarifaiMultiResultDto
    {
        public String status_code { get; set; }
        public String status_msg { get; set; }
        public String local_id { get; set; }
        public String docid { get; set; }
        public ClarifaiIndividualResultDto result { get; set; }
    }
}