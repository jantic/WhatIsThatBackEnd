using System.Collections.Generic;

namespace whatisthatService.Core.Classification.TagFilters
{
    public interface ITagsFilter
    {
        List<ImageTag> Filter(List<ImageTag> tags);
    }
}
