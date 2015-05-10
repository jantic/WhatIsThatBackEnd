using System.Collections.Generic;
using whatisthatService.SpeciesIndentifier.Classification.TagFilters;

namespace whatisthatService.SpeciesIndentifier.Classification
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
