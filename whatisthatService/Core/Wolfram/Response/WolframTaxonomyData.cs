using System;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using whatisthatService.Core.Wolfram.Response.Dto;

namespace whatisthatService.Core.Wolfram.Response
{
    class WolframTaxonomyData
    {
        public static readonly WolframTaxonomyData NULL = new WolframTaxonomyData(null);
        private readonly String _class;
        private readonly String _family;
        private readonly String _genus;
        private readonly String _kingdom;
        private readonly String _order;
        private readonly String _phylum;
        private readonly String _species;

        private WolframTaxonomyData(WolframResponseDto dto)
        {
            if (dto == null)
            {
                _kingdom = "";
                _phylum = "";
                _class = "";
                _order = "";
                _family = "";
                _genus = "";
                _species = "";
            }
            else
            {
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

                _kingdom = results.ContainsKey("kingdom") ? results["kingdom"] : "";
                _phylum = results.ContainsKey("phylum") ? results["phylum"] : "";
                _class = results.ContainsKey("class") ? results["class"] : "";
                _order = results.ContainsKey("order") ? results["order"] : "";
                _family = results.ContainsKey("family") ? results["family"] : "";
                _genus = results.ContainsKey("genus") ? results["genus"] : "";
                _species = results.ContainsKey("species") ? results["species"] : "";
            }
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

        public static WolframTaxonomyData GetInstance(WolframResponseDto dto)
        {
            if (dto == null || dto.Success == null || dto.Success.ToLower() != "true")
            {
                return NULL;
            }
            return new WolframTaxonomyData(dto);
        }
    }
}