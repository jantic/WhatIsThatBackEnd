using System.Collections.Generic;

namespace whatisthatService.Core.Classification.MetaFilters
{
    //Simply sorts probabilites from highest to lowest on the multiple crops of the same image.
    public class MetaProbabilityFilter : IMetaFilter
    {
        public List<SpeciesIdentityResult> Filter(List<SpeciesIdentityResult> candidates)
        {
            candidates.Sort((candidate1, candidate2) => candidate1.LikelySpeciesInfo.GetProbability().CompareTo(candidate2.LikelySpeciesInfo.GetProbability()));
            candidates.Reverse();
            return candidates;
        }
    }
}
