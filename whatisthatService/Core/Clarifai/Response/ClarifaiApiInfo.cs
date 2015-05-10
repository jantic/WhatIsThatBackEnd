using System;
using whatisthatService.Core.Clarifai.Response.Dto;

namespace whatisthatService.Core.Clarifai.Response
{
    public class ClarifaiApiInfo
    {
        private readonly Double _apiVersion;
        private readonly Int64 _maxBatchSize;
        private readonly Int64 _maxImageBytes;
        private readonly Int64 _maxImageSize;
        private readonly Int64 _maxVideoBatchSize;
        private readonly Int64 _maxVideoBytes;
        private readonly Int64 _maxVideoDuration;
        private readonly Int64 _maxVideoSize;
        private readonly Int64 _minImageSize;
        private readonly Int64 _minVideoSize;
        private readonly ClarifaiApiInfoResultDto _sourceDto;

        public ClarifaiApiInfo(ClarifaiApiInfoResultDto dto)
        {
            Int64.TryParse(dto.max_batch_size, out _maxBatchSize);
            Int64.TryParse(dto.max_image_bytes, out _maxImageBytes);
            Int64.TryParse(dto.max_image_size, out _maxImageSize);
            Int64.TryParse(dto.max_video_batch_size, out _maxVideoBatchSize);
            Int64.TryParse(dto.max_video_bytes, out _maxVideoBytes);
            Int64.TryParse(dto.max_video_duration, out _maxVideoDuration);
            Int64.TryParse(dto.max_video_size, out _maxVideoSize);
            Int64.TryParse(dto.min_image_size, out _minImageSize);
            Int64.TryParse(dto.min_video_size, out _minVideoSize);
            Double.TryParse(dto.api_version, out _apiVersion);
            _sourceDto = dto;
        }

        public Int64 MaxImageSize
        {
            get { return _maxImageSize; }
        }

        public Int64 MaxBatchSize
        {
            get { return _maxBatchSize; }
        }

        public Int64 MaxVideoBatchSize
        {
            get { return _maxVideoBatchSize; }
        }

        public Int64 MaxImageBytes
        {
            get { return _maxImageBytes; }
        }

        public Double ApiVersion
        {
            get { return _apiVersion; }
        }

        public Int64 MinImageSize
        {
            get { return _minImageSize; }
        }

        public Int64 MinVideoSize
        {
            get { return _minVideoSize; }
        }

        public Int64 MaxVideoBytes
        {
            get { return _maxVideoBytes; }
        }

        public Int64 MaxVideoSize
        {
            get { return _maxVideoSize; }
        }

        public Int64 MaxVideoDuration
        {
            get { return _maxVideoDuration; }
        }

        public ClarifaiApiInfo Clone()
        {
           return new ClarifaiApiInfo(_sourceDto); 
        }
    }
}