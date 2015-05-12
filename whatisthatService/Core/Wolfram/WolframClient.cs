using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using RestSharp;
using whatisthatService.Core.Classification;
using whatisthatService.Core.Utilities.Caching;
using whatisthatService.Core.Wolfram.Response;
using whatisthatService.Core.Wolfram.Response.Dto;

namespace whatisthatService.Core.Wolfram
{
    ///<summary>Thread safe client for Wolfram Programming Cloud api calls, that caches the calls to long term storage and memory.
    ///</summary>
    public class WolframClient
    {
        private const string BaseUrl = "https://programming.wolframcloud.com";
        private static readonly string NameDataPath = System.Web.Configuration.WebConfigurationManager.AppSettings["wolfram_name_data_path"];
        private static readonly string TaxonomicDataPath = System.Web.Configuration.WebConfigurationManager.AppSettings["wolfram_taxonomy_path"];
        private static readonly string TaxonomicIdImagePath = System.Web.Configuration.WebConfigurationManager.AppSettings["wolfram_taxonomic_id_image_path"];

        private static readonly GenericLongTermCache<WolframResponseDto> CommonNameCache = new GenericLongTermCache<WolframResponseDto>();
        private static readonly GenericLongTermCache<WolframResponseDto> TaxonomicDataCache = new GenericLongTermCache<WolframResponseDto>();
        private static readonly LongTermImageCache TaxonomicIdImageCache = new LongTermImageCache();

        public String GetCommonNameFromScientific(TaxonomicClassification taxonomy)
        {
            //Both are required
            if (String.IsNullOrEmpty(taxonomy.GetGenus()) || String.IsNullOrWhiteSpace(taxonomy.GetSpecies()))
            {
                return "";
            }

            var cacheKey = GenerateCommonNameCacheKey(taxonomy);
            var cachedNameDto = CommonNameCache.Get(cacheKey);

            if (cachedNameDto != null)
            {
                var cachedNameData = WolframCommonNameData.GetInstance(cachedNameDto);
                return cachedNameData.Name;
            }
            var parameters = new List<Parameter>();
            var genusParam = new Parameter { Name = "genus", Value = taxonomy.GetGenus(), Type = ParameterType.QueryString };
            parameters.Add(genusParam);
            var speciesParam = new Parameter { Name = "species", Value = taxonomy.GetSpecies(), Type = ParameterType.QueryString };
            parameters.Add(speciesParam);

            var nameDataDto = ExecuteGetRequest<WolframResponseDto>(NameDataPath, parameters);

            if (nameDataDto != null)
            {
                CommonNameCache.Set(cacheKey, nameDataDto);
            }

            var nameData = WolframCommonNameData.GetInstance(nameDataDto);
            return nameData.Name;
        }

        private String GenerateCommonNameCacheKey(TaxonomicClassification taxonomy)
        {
            var keyBuffer = new StringBuilder();
            keyBuffer.Append(taxonomy.GetGenus());
            keyBuffer.Append("_");
            keyBuffer.Append(taxonomy.GetSpecies());
            return keyBuffer.ToString();
        }

        public TaxonomicClassification GetTaxonomyData(String tag)
        {
            var cacheKey = GenerateTaxonomicDataCacheKey(tag);
            var cachedSpeciesDataDto = TaxonomicDataCache.Get(cacheKey);

            if (cachedSpeciesDataDto != null)
            {
                var cachedTaxonomyData = WolframTaxonomyData.GetInstance(cachedSpeciesDataDto);
                return TaxonomicClassification.GetInstance(cachedTaxonomyData.Kingdom, cachedTaxonomyData.Phylum, 
                    cachedTaxonomyData.Class, cachedTaxonomyData.Order, cachedTaxonomyData.Family,
                    cachedTaxonomyData.Genus, cachedTaxonomyData.Species);
            }

            var parameters = new List<Parameter>();
            var tagParameter = new Parameter {Name = "tag", Value = tag, Type = ParameterType.QueryString};
            parameters.Add(tagParameter);
      
            var taxonomicDataDto = ExecuteGetRequest<WolframResponseDto>(TaxonomicDataPath, parameters);

            if (taxonomicDataDto != null)
            {
                TaxonomicDataCache.Set(cacheKey, taxonomicDataDto);
            }

            var wolframTaxonomyData = WolframTaxonomyData.GetInstance(taxonomicDataDto);
            return TaxonomicClassification.GetInstance(wolframTaxonomyData.Kingdom, wolframTaxonomyData.Phylum, 
                wolframTaxonomyData.Class, wolframTaxonomyData.Order, wolframTaxonomyData.Family,
                wolframTaxonomyData.Genus, wolframTaxonomyData.Species);
        }

        private String GenerateTaxonomicDataCacheKey(String tag)
        {
            return tag.Trim().ToLower();
        }


        public Image GetTaxonomicIdImage(String taxonomicLevel, String scientificName)
        {
            var cacheKey = GenerateTaxonomicIDImageCacheKey(taxonomicLevel, scientificName);
            var cachedImage = TaxonomicIdImageCache.Get(cacheKey);

            if (cachedImage != null)
            {
                return cachedImage;
            }

            var parameters = new List<Parameter>();
            var taxonomicLevelParameter = new Parameter { Name = "taxonomicLevel", Value = taxonomicLevel, Type = ParameterType.QueryString };
            parameters.Add(taxonomicLevelParameter);
            var scientificNameParameter = new Parameter { Name = "scientificName", Value = scientificName, Type = ParameterType.QueryString };
            parameters.Add(scientificNameParameter);
            var imageDataDto = ExecuteGetRequest<WolframResponseDto>(TaxonomicIdImagePath, parameters);

            if (imageDataDto == null) return null;
            var image = ConvertResponseToImage(imageDataDto);
            TaxonomicIdImageCache.Set(cacheKey, image);
            return image;
        }

        private Image ConvertResponseToImage(WolframResponseDto dto)
        {
            const String pattern = "(URL\"\\s+->\\s+\")(.*?)(\",\\s+\"Source)";
            var regex = new Regex(pattern);
            if (!regex.IsMatch(dto.Result)) return default(Image);
            var url = regex.Match(dto.Result).Groups[2].ToString();
            return GetImageFromUrl(url);
        }

        private Image GetImageFromUrl(String url)
        {
            var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);

            using (var httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
            {
                using (var stream = httpWebReponse.GetResponseStream())
                {
                    if (stream != null) return Image.FromStream(stream);
                }
            }

            return null;
        }

        private String GenerateTaxonomicIDImageCacheKey(String taxonomicLevel, String scientificName)
        {
            return taxonomicLevel + "." + scientificName;
        }

        private T ExecuteGetRequest<T>(String apiPath, List<Parameter> parameters) where T : new()
        {
            var request = BuildRestRequest(apiPath, Method.GET);
            request.Parameters.AddRange(parameters);
            var client = BuildRestClient();
            return ExecuteRequestRobustly<T>(client, request);
        }

        private T ExecuteRequestRobustly<T>(IRestClient client, IRestRequest request)
        {
            IRestResponse response = null;
            var attempts = 3;
            var success = false;

            while (attempts > 0 && !success)
            {
                attempts--;
                success = AttemptRequestExecution(client, request, out response);
            }

            if (response == null)
            {
                const string message = "Error retrieving response- Failed to communicate with Wolfram server.";
                var e = new ApplicationException(message);
                throw e;
            }

            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response.  Check inner details for more info.";
                var e = new ApplicationException(message, response.ErrorException);
                throw e;
            }

            //Using this json parser instead of RestSharp's implicit parsing in Execute's result
            //because it crashes attempting to parse the results.
            return JsonConvert.DeserializeObject<T>(response.Content);
        }

        private Boolean AttemptRequestExecution(IRestClient client, IRestRequest request, out IRestResponse response)
        {
            try
            {
                response = client.Execute(request);
                return true;
            }
            catch (Exception)
            {
                response = null;
                return false;
            }
        }

        private RestClient BuildRestClient()
        {
            var client = new RestClient {BaseUrl = new Uri(BaseUrl)};
            return client;
        }

        private RestRequest BuildRestRequest(String apiPath, Method method)
        {
            var request = new RestRequest(apiPath, method) {RequestFormat = DataFormat.Json};
            return request;
        }
    }
}