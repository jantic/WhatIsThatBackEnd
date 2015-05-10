using System.Collections.Generic;

namespace whatisthatService.Core.Classification.MetaFilters
{
    public interface IMetaFilter
    {
        List<SpeciesIdentityResult> Filter(List<SpeciesIdentityResult> candidates);
    }

}
