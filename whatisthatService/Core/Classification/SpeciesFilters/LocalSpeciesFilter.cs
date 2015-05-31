using System;
using System.Collections.Generic;
using System.Linq;
using System.Spatial;
using whatisthatService.Core.Geography;
using whatisthatService.Core.Wolfram;

namespace whatisthatService.Core.Classification.SpeciesFilters
{
    public class LocalSpeciesFilter : ISpeciesFilter
    {
        private readonly IUCNClient _iucnClient = new IUCNClient();
        private readonly WolframClient _wolframClient = new WolframClient();

        public List<SpeciesInfo> Filter(List<SpeciesInfo> speciesInfos, GeographyPoint coordinates)
        {
            var localTaxonomies = _iucnClient.GetLocalSpeciesTaxonomiesList(coordinates);
            return GetBestMatches(speciesInfos, localTaxonomies);
        }

        private List<SpeciesInfo> SortByTagNameMatchesFirst(List<SpeciesInfo> matches)
        {
            var nameFilteredBestMatches = new List<SpeciesInfo>();

            foreach (var match in matches)
            {
                var matchName = match.GetName().ToLower().Trim();
                var tag = match.GetTagName();

                if (matchName.Contains(tag)
                    || tag.Contains(matchName))
                {
                    if (nameFilteredBestMatches.Find(speciesInfo => 
                        speciesInfo.GetName().Equals(match.GetName(), StringComparison.InvariantCultureIgnoreCase)) == null)
                    {
                        nameFilteredBestMatches.Add(match);
                    }
                }
            }

            foreach (var match in matches)
            {
                if (!nameFilteredBestMatches.Contains(match))
                {
                    nameFilteredBestMatches.Add(match);
                }
            }

            return nameFilteredBestMatches;
        }

        private List<SpeciesInfo> GetBestMatchingLocalSpecies(List<SpeciesInfo> candidates, List<TaxonomicClassification> localTaxonomies)
        {
            var bestMatchTaxonomies = new List<TaxonomicClassification>();
            var selectedCandidate = candidates.First();

            foreach (var candidate in candidates)
            {
                bestMatchTaxonomies = GetBestMatchingTaxonomies(candidate, localTaxonomies);
                if (bestMatchTaxonomies.Count <= 0) continue;
                selectedCandidate = candidate;
                break;
            }

            if (bestMatchTaxonomies.Count == 0)
            {
                return new List<SpeciesInfo> { selectedCandidate };
            }

            var bestMatchSpeciesInfos = new List<SpeciesInfo>();

            foreach (var taxonomy in bestMatchTaxonomies)
            {
                var commonName = _wolframClient.GetCommonNameFromScientific(taxonomy);
                var scientificName = taxonomy.GetGenus() + " " + taxonomy.GetSpecies();
                var finalName = String.IsNullOrEmpty(commonName)
                    ? (String.IsNullOrEmpty(selectedCandidate.GetTagName()) ? scientificName : selectedCandidate.GetTagName())
                    : commonName;
                var speciesInfo = SpeciesInfo.GetInstance(taxonomy, finalName, selectedCandidate.GetTagName(), selectedCandidate.GetProbability());
                bestMatchSpeciesInfos.Add(speciesInfo);
            }

            return bestMatchSpeciesInfos;
        }

        private List<SpeciesInfo> GetBestMatches(List<SpeciesInfo> candidates, List<TaxonomicClassification> localTaxonomies)
        {

            if (candidates.Count == 0)
            {
                return candidates;
            }

            var bestMatchSpeciesInfos = GetBestMatchingLocalSpecies(candidates, localTaxonomies);
            return SortByTagNameMatchesFirst(bestMatchSpeciesInfos);
        }

        private List<TaxonomicClassification> GetBestMatchingTaxonomies(SpeciesInfo candidate, List<TaxonomicClassification> localTaxonomies)
        {
            var speciesMatches = GetAllMatchesForSpecies(candidate, localTaxonomies);
            if (speciesMatches.Count > 0)
            {
                return speciesMatches;
            }

            if (candidate.GetName().ToLower() == "human")
            {
                return new List<TaxonomicClassification>();
                //TODO:  hack to prevent overzealous matching for humans.  Need to figure out a better way...
            }

            var genusMatches = GetAllMatchesForGenus(candidate, localTaxonomies);
            if (genusMatches.Count > 0)
            {
                return genusMatches;
            }

            var familyMatches = GetAllMatchesForFamily(candidate, localTaxonomies);
            if (familyMatches.Count > 0)
            {
                return familyMatches;
            }

            return new List<TaxonomicClassification>();
        }

        private List<TaxonomicClassification> GetAllMatchesForSpecies(SpeciesInfo candidate, List<TaxonomicClassification> localTaxonomies)
        {
            return localTaxonomies.Where(localTaxonomy => String.Equals(localTaxonomy.GetSpecies(), candidate.Taxonomy.GetSpecies(), StringComparison.InvariantCultureIgnoreCase)).ToList();
        }

        //Humans are a special case here- only allow for exact species matches;  Otherwise, human identification tends to overfire
        private List<TaxonomicClassification> GetAllMatchesForGenus(SpeciesInfo candidate, List<TaxonomicClassification> localTaxonomies)
        {
            return localTaxonomies.Where(localTaxonomy => 
                (String.Equals(localTaxonomy.GetGenus(), candidate.Taxonomy.GetGenus(), StringComparison.InvariantCultureIgnoreCase)
                && !String.Equals(localTaxonomy.GetSpecies(), "Sapiens", StringComparison.InvariantCultureIgnoreCase))).ToList();
        }

        //Humans are a special case here- only allow for exact species matches;  Otherwise, human identification tends to overfire
        private List<TaxonomicClassification> GetAllMatchesForFamily(SpeciesInfo candidate, List<TaxonomicClassification> localTaxonomies)
        {
            return localTaxonomies.Where(localTaxonomy => 
                (String.Equals(localTaxonomy.GetFamily(), candidate.Taxonomy.GetFamily(), StringComparison.InvariantCultureIgnoreCase) 
                && !String.Equals(localTaxonomy.GetSpecies(), "Sapiens", StringComparison.InvariantCultureIgnoreCase))).ToList();
        }
    }
}
