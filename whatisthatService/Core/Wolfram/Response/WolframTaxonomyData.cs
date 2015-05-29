using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using whatisthatService.Core.Wolfram.Response.Dto;

namespace whatisthatService.Core.Wolfram.Response
{
    public class WolframTaxonomyData
    {
        public static readonly WolframTaxonomyData NULL = new WolframTaxonomyData("","","","","","","");
        private readonly String _class;
        private readonly String _family;
        private readonly String _genus;
        private readonly String _kingdom;
        private readonly String _order;
        private readonly String _phylum;
        private readonly String _species;

        public static WolframTaxonomyData GetInstance(WolframResponseDto dto)
        {
            if (dto == null || dto.Success == null || dto.Success.ToLower() != "true")
            {
                return NULL;
            }

            const string pattern = "(Entity)(\\[.*?\\])";
            var matches = Regex.Matches(dto.Result, pattern);

            var results = (from Match match in matches
                select match.Groups[2].ToString()
                into arrayString
                select JsonConvert.DeserializeObject<String[]>(arrayString)
                into entityResult
                select entityResult.Length > 1 ? entityResult[1] : ""
                into rawValue
                where rawValue.Contains(":")
                select rawValue.Split(':')).
                ToDictionary(splitResult => splitResult[0].ToLower().Trim(), splitResult => splitResult[1].Trim());

            var kingdom = results.ContainsKey("kingdom") ? results["kingdom"] : "";
            var phylum = results.ContainsKey("phylum") ? results["phylum"] : "";
            var tclass = results.ContainsKey("class") ? results["class"] : "";
            var order = results.ContainsKey("order") ? results["order"] : "";
            var family = results.ContainsKey("family") ? results["family"] : "";
            var genus = results.ContainsKey("genus") ? results["genus"] : "";
            //For some reason, Wolfram encodes species names as Genus + Species combined. Derp!
            var species = results.ContainsKey("species") ? results["species"].Replace(genus, "") : "";

            return GetInstance(kingdom, phylum, tclass, order, family, genus, species);
        }

        public static WolframTaxonomyData GetInstance(String kingdom, String phylum, String tclass, String order, String family, String genus, String species)
        {
            if (String.IsNullOrEmpty(kingdom))
            {
                return NULL;
            }

            return new WolframTaxonomyData(kingdom, phylum, tclass, order, family, genus, species);
        }

        private WolframTaxonomyData(String kingdom, String phylum, String tclass, String order, String family, String genus, String species)
        {
            _kingdom = kingdom;
            _phylum = phylum;
            _class = tclass;
            _order = order;
            _family = family;
            _genus = genus;
            _species = species;
        }

        public String Kingdom
        {
            get { return _kingdom; }
        }

        public String Phylum
        {
            get { return _phylum; }
        }

        public String Class
        {
            get { return _class; }
        }

        public String Order
        {
            get { return _order; }
        }

        public String Family
        {
            get { return _family; }
        }

        public String Genus
        {
            get { return _genus; }
        }

        public String Species
        {
            get { return _species; }
        }
    }
}