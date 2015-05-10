using System.Collections.Generic;
using whatisthatService.Core.Classification.TagFilters;

namespace whatisthatService.Core.Classification
{
    class TagsFilterFactory
    {
        private static readonly List<ITagsFilter> FiltersList = new List<ITagsFilter> { new TagBlacklistSpeciesFilter(), new HumanTagsFilter()};

        public List<ITagsFilter> GetOrderedFilters()
        {
            return FiltersList;
        }
    }
}
