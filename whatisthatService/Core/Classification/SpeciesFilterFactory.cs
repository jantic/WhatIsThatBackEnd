﻿using System;
using System.Collections.Generic;
using whatisthatService.Core.Classification.SpeciesFilters;

namespace whatisthatService.Core.Classification
{
    class SpeciesFilterFactory
    {
        private readonly List<ISpeciesFilter> _filtersList;

        public SpeciesFilterFactory(Boolean geoContextMode)
        {
            _filtersList = new List<ISpeciesFilter> { new UniqueTaxonomyFilter(), new OutlierSpeciesFilter(), new SpeciesProbabilityFilter()};
            if (geoContextMode)
            {
                _filtersList.Add(new LocalSpeciesFilter());
            }
        }

        public List<ISpeciesFilter> GetOrderedFilters()
        {
            return _filtersList;
        }
    }
}
