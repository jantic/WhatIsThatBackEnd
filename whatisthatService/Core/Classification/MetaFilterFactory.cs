using System.Collections.Generic;
using whatisthatService.Core.Classification.MetaFilters;

namespace whatisthatService.Core.Classification
{
    class MetaFilterFactory
    {
        private readonly List<IMetaFilter> _filtersList;

        public MetaFilterFactory()
        {
            _filtersList = new List<IMetaFilter> {new MetaUniqueTaxonomyFilter(), new MetaProbabilityFilter(), new MetaHumanFilter()};
        }

        public List<IMetaFilter> GetOrderedFilters()
        {
            return _filtersList;
        }
    }
}
