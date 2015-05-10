using System.Collections.Generic;
using System.Linq;

namespace whatisthatService.SpeciesIndentifier.Classification.MetaFilters
{
    public class MetaHumanFilter : IMetaFilter
    {
        //This compensates for overzealous human classification by applying the standard that at least two
        //of the crops of the image must rate as human.  The logic:  A head/face by itself should register as human
        //as well as the rest of the body + head.  This gets rid of a lot of false positives for humans.
        public List<SpeciesIdentityResult> Filter(List<SpeciesIdentityResult> candidates)
        {
            var topCandidate = candidates.FirstOrDefault();

            if (topCandidate == null || !topCandidate.LikelySpeciesInfo.Taxonomy.Equals(TaxonomicClassification.HUMAN))
            {
                return candidates;
            }

            for (var index = 1; index < candidates.Count; index++)
            {
                var candidate = candidates[index];
                if (candidate.LikelySpeciesInfo.Taxonomy.Equals(TaxonomicClassification.HUMAN))
                {
                    return candidates;
                }
            }

            return new List<SpeciesIdentityResult>();
        }
    }
}
