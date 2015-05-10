using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Spatial;

namespace whatisthatService.SpeciesIndentifier.Classification.SpeciesFilters
{
    public class SpeciesProbabilityFilter : ISpeciesFilter
    {
        private static readonly ImmutableDictionary<String, Double> ProbabilityMins = LoadProbabilityMins();

        private static ImmutableDictionary<String, Double> LoadProbabilityMins()
        {
            //TODO:  Probably want a more formal means of data storage and of determining these values   
            var probabilityMins = new Dictionary<String, Double>
            {
                {"human", 0.97},
                {"elephant", 0.96},
                {"african elephant", 0.96},
                {"default", 0.80}
            };
            return probabilityMins.ToImmutableDictionary();
        }

        public List<SpeciesInfo> Filter(List<SpeciesInfo> speciesInfos, GeographyPoint coordinates)
        {
            var sortedSpeciesInfosArray = new SpeciesInfo[speciesInfos.Count];
            speciesInfos.CopyTo(sortedSpeciesInfosArray);
            var sortedSpeciesInfos = new List<SpeciesInfo>(sortedSpeciesInfosArray);

            sortedSpeciesInfos.Sort((candidate1, candidate2) =>
                candidate2.GetProbability().CompareTo(candidate1.GetProbability()));

            return sortedSpeciesInfos.Where(speciesInfo => speciesInfo.GetProbability() >= GetMinProbability(speciesInfo)).ToList();
        }

        private Double GetMinProbability(SpeciesInfo speciesInfo)
        {
            var key = speciesInfo.GetName().ToLower();
            return ProbabilityMins.ContainsKey(key) ? ProbabilityMins[key] : ProbabilityMins["default"];
        }
    }
}
