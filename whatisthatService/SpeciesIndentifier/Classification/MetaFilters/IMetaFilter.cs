using System.Collections.Generic;

namespace whatisthatService.SpeciesIndentifier.Classification.MetaFilters
{
    public interface IMetaFilter
    {
        List<SpeciesIdentityResult> Filter(List<SpeciesIdentityResult> candidates);
    }

}
