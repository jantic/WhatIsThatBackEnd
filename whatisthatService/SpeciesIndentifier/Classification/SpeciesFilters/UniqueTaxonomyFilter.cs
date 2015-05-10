using System.Collections.Generic;
using System.Linq;
using System.Spatial;

namespace whatisthatService.SpeciesIndentifier.Classification.SpeciesFilters
{
    public class UniqueTaxonomyFilter : ISpeciesFilter
    {
        public List<SpeciesInfo> Filter(List<SpeciesInfo> speciesInfos, GeographyPoint coordinates)
        {
            return (from candidateToFilter in speciesInfos let isGeneralization = speciesInfos.
                        Any(candidateToFilter.IsGeneralizationOfThis) 
                    where !isGeneralization select candidateToFilter).ToList();
        }
    }
}
