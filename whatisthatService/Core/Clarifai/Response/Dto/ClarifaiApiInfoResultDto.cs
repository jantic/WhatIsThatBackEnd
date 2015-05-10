using System;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.Core.Clarifai.Response.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class ClarifaiApiInfoResultDto
    {
        public String max_image_size { get; set; }
        public String max_batch_size { get; set; }
        public String max_video_batch_size { get; set; }
        public String max_image_bytes { get; set; }
        public String api_version { get; set; }
        public String min_image_size { get; set; }
        public String min_video_size { get; set; }
        public String max_video_bytes { get; set; }
        public String max_video_size { get; set; }
        public String max_video_duration { get; set; }
    }
}