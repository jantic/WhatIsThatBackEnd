using System;
using System.Collections.Generic;
using System.Linq;
using System.Spatial;

namespace whatisthatService.SpeciesIndentifier.Classification.SpeciesFilters
{
    public class OutlierSpeciesFilter : ISpeciesFilter
    {
        public List<SpeciesInfo> Filter(List<SpeciesInfo> speciesInfos, GeographyPoint coordinates)
        {
            return speciesInfos.Where(candidate => !IsOutlier(candidate.Taxonomy, 
                speciesInfos.Select(speciesInfo => candidate.Taxonomy).ToList())).ToList();
        }


        public Boolean IsOutlier(TaxonomicClassification toCompare, List<TaxonomicClassification> taxonomies)
        {
            if (taxonomies.Count < 3)
            {
                //Not enough to determine
                return false;
            }

            var usedClassifications = new List<TaxonomicClassification.BiologicalClassification>
            {
                TaxonomicClassification.BiologicalClassification.Kingdom,
                TaxonomicClassification.BiologicalClassification.Phylum
            };

            foreach (var classification in usedClassifications)
            {
                var hits = new Dictionary<String, int>();
                var toCompareTaxonomicInfo = toCompare.GetOrderedTaxonomicInfo();
                var myValue = toCompareTaxonomicInfo.Find(item => item.Key == classification).Value;

                if (String.IsNullOrWhiteSpace(myValue))
                {
                    return false;
                }

                foreach (var candidate in taxonomies)
                {
                    var taxonomicInfo = candidate.GetOrderedTaxonomicInfo();
                    var candidateValue = taxonomicInfo.Find(item => item.Key == classification).Value;

                    if (String.IsNullOrEmpty(candidateValue))
                    {
                        continue;
                    }

                    if (!hits.ContainsKey(candidateValue))
                    {
                        hits.Add(candidateValue, 1);
                    }
                    else
                    {
                        hits[candidateValue]++;
                    }
                }

                if (!hits.ContainsKey(myValue))
                {
                    return false;
                }

                if (hits.Values.Max() > 1 && hits[myValue] == 1)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
