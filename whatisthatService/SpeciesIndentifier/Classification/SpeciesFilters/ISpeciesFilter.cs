using System.Collections.Generic;
using System.Spatial;

namespace whatisthatService.SpeciesIndentifier.Classification.SpeciesFilters
{
    public interface ISpeciesFilter
    {
        List<SpeciesInfo> Filter(List<SpeciesInfo> speciesInfos, GeographyPoint coordinates);
    }
}
