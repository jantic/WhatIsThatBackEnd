using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using whatisthatService.Core.Clarifai.Response.Dto;
using whatisthatService.Core.Classification;

namespace whatisthatService.Core.Clarifai.Response
{
    public class ClarifaiTagsCollection
    {
        private readonly ImmutableList<ImageTag> _imageTags;

        public ClarifaiTagsCollection(ClarifaiTagResultDto dto)
        {
            _imageTags = ImmutableList.Create(ReadAllImageTags(dto).ToArray());
        }

        public List<ImageTag> ImageTags
        {
            get { return _imageTags.ToList(); }
        }

        private List<ImageTag> ReadAllImageTags(ClarifaiTagResultDto dto)
        {
            var tags = new List<ImageTag>();

            if (dto == null)
            {
                return tags;
            }

            for (var index = 0; index < dto.classes.Count; index++)
            {
                var name = dto.classes[index];
                var probability = index < dto.probs.Count
                    ? (String.IsNullOrEmpty(dto.probs[index]) ? 0 : Double.Parse(dto.probs[index]))
                    : 0;
                tags.Add(new ImageTag(name, probability));
            }

            return tags;
        }
    }
}