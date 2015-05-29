using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace whatisthatService.Core.Classification
{
    public class TaxonomicClassification
    {
        public static readonly TaxonomicClassification NULL = new TaxonomicClassification("","","","","","","");
        public static readonly TaxonomicClassification HUMAN = new TaxonomicClassification("ANIMALIA", "CHORDATA", "MAMMALIA", "PRIMATES", "HOMINIDAE", "Homo", "Sapiens");

        public static TaxonomicClassification GetInstance(String kingdom, String phylum, String bioClass, String order, String family, String genus, String species)
        {
            if (String.IsNullOrEmpty(kingdom))
            {
                return NULL;
            }

            return new TaxonomicClassification(kingdom, phylum, bioClass, order, family, genus, species);
        }

        public enum BiologicalClassification
        {
            Kingdom,
            Phylum,
            Class,
            Order,
            Family,
            Genus,
            Species
        }

        private readonly ImmutableList<KeyValuePair<BiologicalClassification, String>> _taxonomy;
        //For performance:
        private readonly String _kingdom;
        private readonly String _phylum;
        private readonly String _bioClass;
        private readonly String _order;
        private readonly String _family;
        private readonly String _genus;
        private readonly String _species;

        private TaxonomicClassification(String kingdom, String phylum, String bioClass, String order, String family, String genus, String species)
        {
            _taxonomy = ImmutableList.Create(new KeyValuePair<BiologicalClassification, String>(BiologicalClassification.Kingdom, kingdom), 
                new KeyValuePair<BiologicalClassification, String>(BiologicalClassification.Phylum, phylum), 
                new KeyValuePair<BiologicalClassification, String>(BiologicalClassification.Class, bioClass), 
                new KeyValuePair<BiologicalClassification, String>(BiologicalClassification.Order, order),
                new KeyValuePair<BiologicalClassification, String>(BiologicalClassification.Family, family), 
                new KeyValuePair<BiologicalClassification, String>(BiologicalClassification.Genus, genus), 
                new KeyValuePair<BiologicalClassification, String>(BiologicalClassification.Species, species));

            _kingdom = kingdom;
            _phylum = phylum;
            _bioClass = bioClass;
            _order = order;
            _family = family;
            _genus = genus;
            _species = species;
        }

        public List<KeyValuePair<BiologicalClassification, String>> GetOrderedTaxonomicInfo()
        {
            return _taxonomy.ToList();
        }

        public String GetSpecies()
        {
            return _species;
        }

        public String GetGenus()
        {
            return _genus;
        }

        public String GetClass()
        {
            return _bioClass;
        }

        public String GetFamily()
        {
            return _family;
        }

        public String GetPhylum()
        {
            return _phylum;
        }

        public String GetOrder()
        {
            return _order;
        }

        public String GetKingdom()
        {
            return _kingdom;
        }

        public Boolean Equals(TaxonomicClassification taxonomicClassification)
        {
            for (var index = 0; index < _taxonomy.Count; index++)
            {
                var myClassification = _taxonomy[index].Value;
                var theirClassification = taxonomicClassification._taxonomy[index].Value;

                if (!String.Equals(myClassification, theirClassification, StringComparison.InvariantCultureIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public Boolean IsGeneralizationOfThis(TaxonomicClassification taxonomicClassification)
        {
            if (this == taxonomicClassification)
            {
                return false;
            }

            for (var index = 0; index < _taxonomy.Count; index++)
            {
                var myClassification = _taxonomy[index].Value;
                var theirClassification = taxonomicClassification._taxonomy[index].Value;

                if (String.IsNullOrEmpty(myClassification) && !String.IsNullOrEmpty(theirClassification))
                {
                    return true;
                }

                if (!String.IsNullOrEmpty(myClassification) && String.IsNullOrEmpty(theirClassification))
                {
                    return false;
                }

                if (myClassification != theirClassification)
                {
                    return false;
                }
            }

            return false;

        }

      
        public KeyValuePair<BiologicalClassification, String> GetMostSpecificClassification()
        {
            for (var index = _taxonomy.Count - 1; index >= 0; index--)
            {
                var classification = _taxonomy[index];
                if (!String.IsNullOrEmpty(classification.Value))
                {
                    return classification;
                }
            }

            return _taxonomy[0];
        }

    }
}
