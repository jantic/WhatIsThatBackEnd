using System;
using System.Drawing;
using whatisthatService.Core.Wolfram;

namespace whatisthatService.Core.Classification
{
    public class SpeciesInfo
    {

        private readonly String _name;
        private readonly String _tagName;
        private readonly Double _independentProbability;
        private readonly TaxonomicClassification _taxonomy;
        //Ordered according to hierarchy

        public static SpeciesInfo NULL = new SpeciesInfo(TaxonomicClassification.NULL, "", "", 0);

        public static SpeciesInfo GetInstance(TaxonomicClassification taxonomy, String commonName, String tagName, Double probability)
        {
            if (String.IsNullOrEmpty(tagName) || taxonomy == TaxonomicClassification.NULL)
            {
                return NULL;
            }

            return new SpeciesInfo(taxonomy, commonName, tagName, probability);
        }

        private SpeciesInfo(TaxonomicClassification taxonomy, String commonName, String tagName, Double probability)
        {
            _name = commonName;
            _tagName = tagName;
            _independentProbability = probability;
            _taxonomy = taxonomy;
        }

        public TaxonomicClassification Taxonomy
        {
            get { return _taxonomy; }
        }

        public String GetTagName()
        {
            return _tagName;
        }

        public String GetName()
        {
            return _name;
        }


        public Double GetProbability()
        {
            return _independentProbability;
        }

        public Boolean IsGeneralizationOfThis(SpeciesInfo candidate)
        {
            return _taxonomy.IsGeneralizationOfThis(candidate._taxonomy);
        }

        public Image GetImage()
        {
            var classification = _taxonomy.GetMostSpecificClassification();
            if (String.IsNullOrEmpty(classification.Value)) return null;
            var wolframClient = new WolframClient();
            return wolframClient.GetTaxonomicIdImage(classification.Key.ToString(), Capitalise(classification.Value));
        }

        private string Capitalise(string str)
        {
            if (String.IsNullOrEmpty(str))
                return String.Empty;
            return Char.ToUpper(str[0]) + str.Substring(1).ToLower();
        }
    }
}