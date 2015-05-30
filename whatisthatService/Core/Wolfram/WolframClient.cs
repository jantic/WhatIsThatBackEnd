using System;
using System.Collections.Generic;
using System.Web.Configuration;
using Newtonsoft.Json;
using RestSharp;
using whatisthatService.Core.Classification;
using whatisthatService.Core.Wolfram.Response;
using whatisthatService.Core.Wolfram.Response.Dto;

namespace whatisthatService.Core.Wolfram
{
    ///<summary>Thread safe client for Wolfram Programming Cloud api calls, that caches the calls to long term storage and memory.
    ///</summary>
    public class WolframClient
    {
        private const string BaseUrl = "https://programming.wolframcloud.com";
        private static readonly string CommonNameDataPath = WebConfigurationManager.AppSettings["wolfram_name_data_path"];
        private static readonly string TaxonomicDataPath = WebConfigurationManager.AppSettings["wolfram_taxonomy_path"];

        private static readonly WolframTaxonomyToNameCache CommonNameCache = new WolframTaxonomyToNameCache();
        private static readonly WolframTagToTaxonomyCache TaxonomicDataCache = new WolframTagToTaxonomyCache();

        public String GetCommonNameFromScientific(TaxonomicClassification taxonomy)
        {
            //Both are required
            if (String.IsNullOrEmpty(taxonomy.GetGenus()) || String.IsNullOrWhiteSpace(taxonomy.GetSpecies()))
            {
                return "";
            }

            var cachedNameInfo = CommonNameCache.Get(taxonomy);

            if (cachedNameInfo != null)
            {
                return cachedNameInfo.Name;
            }

            var parameters = new List<Parameter>();
            var genusParam = new Parameter { Name = "genus", Value = taxonomy.GetGenus(), Type = ParameterType.QueryString };
            parameters.Add(genusParam);
            var speciesParam = new Parameter { Name = "species", Value = taxonomy.GetSpecies(), Type = ParameterType.QueryString };
            parameters.Add(speciesParam);

            var nameDataDto = ExecuteGetRequest<WolframResponseDto>(CommonNameDataPath, parameters);

            if (nameDataDto != null)
            {
                var nameData = WolframCommonNameData.GetInstance(nameDataDto);
                CommonNameCache.Set(taxonomy, nameData);
                return nameData.Name;
            }

            return "";
        }
        public TaxonomicClassification GetTaxonomyData(String tag)
        {
            var cachedTaxonomyData = TaxonomicDataCache.Get(tag);

            if (cachedTaxonomyData != null)
            {
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
                var wolframTaxonomyData = WolframTaxonomyData.GetInstance(taxonomicDataDto);
                TaxonomicDataCache.Set(tag, wolframTaxonomyData);

                return TaxonomicClassification.GetInstance(wolframTaxonomyData.Kingdom, wolframTaxonomyData.Phylum,
                    wolframTaxonomyData.Class, wolframTaxonomyData.Order, wolframTaxonomyData.Family,
                    wolframTaxonomyData.Genus, wolframTaxonomyData.Species);
            }

            var nullData = WolframTaxonomyData.NULL;

            return TaxonomicClassification.GetInstance(nullData.Kingdom, nullData.Phylum,
                nullData.Class, nullData.Order, nullData.Family,
                nullData.Genus, nullData.Species);

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