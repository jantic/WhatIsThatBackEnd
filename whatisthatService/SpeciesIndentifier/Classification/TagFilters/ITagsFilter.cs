using System.Collections.Generic;

namespace whatisthatService.SpeciesIndentifier.Classification.TagFilters
{
    public interface ITagsFilter
    {
        List<ImageTag> Filter(List<ImageTag> tags);
    }
}
