using System.Collections.Generic;
using System.Spatial;

namespace whatisthatService.Core.Classification.SpeciesFilters
{
    public interface ISpeciesFilter
    {
        List<SpeciesInfo> Filter(List<SpeciesInfo> speciesInfos, GeographyPoint coordinates);
    }
}
