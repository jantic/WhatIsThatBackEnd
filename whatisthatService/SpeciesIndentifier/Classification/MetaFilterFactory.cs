using System.Collections.Generic;
using whatisthatService.SpeciesIndentifier.Classification.MetaFilters;

namespace whatisthatService.SpeciesIndentifier.Classification
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
