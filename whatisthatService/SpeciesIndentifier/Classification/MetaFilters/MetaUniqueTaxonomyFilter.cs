using System.Collections.Generic;
using System.Linq;

namespace whatisthatService.SpeciesIndentifier.Classification.MetaFilters
{
    //Gets rid of repetive generalizations of taxonomic classifications.  For example, if both mammal
    //and elephant show up, get rid of mammal
    public class MetaUniqueTaxonomyFilter : IMetaFilter
    {
        public List<SpeciesIdentityResult> Filter(List<SpeciesIdentityResult> candidates)
        {
            return (from candidate in candidates 
                    let isGeneralization = candidates.Any(
                    candidateToCompare => candidate.LikelySpeciesInfo.IsGeneralizationOfThis(
                        candidateToCompare.LikelySpeciesInfo)) 
                    where !isGeneralization 
                    select candidate).ToList();
        }
    }
}
