using System;
using System.Collections.Specialized;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using RestSharp.Contrib;
using whatisthatService.Core.Clarifai.Exceptions;
using whatisthatService.Core.Clarifai.Response;
using whatisthatService.Core.Clarifai.Response.Dto;
using whatisthatService.Core.Utilities.ImageProcessing;

namespace whatisthatService.Core.Clarifai
{
    ///<summary>Thread safe client for Clarifai api calls, that caches the calls to long term storage and memory.
    ///</summary>
    public class ClarifaiClient
    {
        private static readonly String ClientId = System.Web.Configuration.WebConfigurationManager.AppSettings["clarifai_api_id"];
        private static readonly String ClientSecret = System.Web.Configuration.WebConfigurationManager.AppSettings["clarifai_api_secret"];

        private const String BaseUrl = "https://api.clarifai.com";
        private const String InfoPath = "/v1/info/";
        private const String MultiPartPath = "/v1/multiop/";
        private const String TokenPath = "/v1/token/";
        private const Double ThrottleWaitSecondsDefault = 10;

        //Needs lock
        private static ClarifaiApiInfo _apiInfo;
        private readonly static Object ApiInfoLock = new Object();
        //Needs lock
        private String _accessToken;
        private readonly static Object AccessTokenLock = new Object();

        public ClarifaiTagsCollection GetTagsInfo(Bitmap image)
        {
            var resizedImage = ResizeImageIfNeeded(image);
            var imagePngByteArray = ImageConversion.ImageToPngByteArray(resizedImage);
            var result = ExecutePostRequest<ClarifaiResponseDto>(MultiPartPath, imagePngByteArray, "tag");
            var tagsResult = result.results == null ? null : result.results[0].result.tag;
            return new ClarifaiTagsCollection(tagsResult);
        }

        private Bitmap ResizeImageIfNeeded(Bitmap image)
        {
            var apiInfo = GetApiInfo();
            return ImageConversion.ResizeImageIfNeeded(image, Convert.ToInt32(apiInfo.MinImageSize), Convert.ToInt32(apiInfo.MaxImageSize));
        }

        public ClarifaiApiInfo GetApiInfo()
        {
            lock (ApiInfoLock)
            {
                if (_apiInfo != null) return _apiInfo.Clone();
                var result = ExecuteGetRequest<ClarifaiMetaApiInfoResponseDto>(InfoPath);
                _apiInfo = new ClarifaiApiInfo(result.results);

                return _apiInfo.Clone();
            }
        }

        private T ExecuteGetRequest<T>(String apiPath) where T : new()
        {
            var attempts = 3;

            while (attempts > 0)
            {
                attempts--;

                try
                {
                    var request = BuildRestRequest(apiPath, Method.GET);
                    var client = BuildRestClient();
                    return ExecuteRequestRobustly<T>(client, request);
                }
                catch (ExpiredTokenError)
                {
                    //Gets new token for next attempt
                    GetAccessToken(true);
                }
            }

            return default(T);
        }

        private T ExecutePostRequest<T>(String apiPath, byte[] imageBytes, String operation) where T : new()
        {
            var attempts = 3;

            while (attempts > 0)
            {
                attempts--;

                try
                {
                    var request = BuildRestRequest(apiPath, Method.POST);
                    request.AddParameter("op", operation);
                    request.AddFile("encoded_data", imageBytes, "image");
                    var client = BuildRestClient();
                    return ExecuteRequestRobustly<T>(client, request);
                }
                catch (ExpiredTokenError)
                {
                    //Gets new token for next attempt
                    GetAccessToken(true);
                }
            }

            return default(T);
        }

        private T ExecuteRequestRobustly<T>(IRestClient client, IRestRequest request) where T : new()
        {
            IRestResponse response = null;
            var attempts = 3;
            var success = false;

            while (attempts > 0 && !success)
            {
                attempts--;
                try
                {
                    success = AttemptRequestExecution(client, request, out response);
                }
                catch (ApiThrottleError e)
                {
                    Thread.Sleep(Convert.ToInt32(e.WaitSeconds*1000));
                }
            }

            if (response == null)
            {
                const string message = "Error retrieving response- Failed to communicate with Clarifai server.";
                var clarifaiException = new ApplicationException(message);
                throw clarifaiException;
            }

            if (response.ErrorException != null)
            {
                const string message = "Error retrieving response.  Check inner details for more info.";
                var clarifaiException = new ApplicationException(message, response.ErrorException);
                throw clarifaiException;
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
            catch (HttpListenerException e)
            {
                switch (e.ErrorCode)
                {
                    case 429:
                        var waitTimeString = e.Data["X-Throttle-Wait-Seconds"].ToString();
                        var waitTime = String.IsNullOrEmpty(waitTimeString)
                            ? ThrottleWaitSecondsDefault
                            : Double.Parse(waitTimeString);
                        throw new ApiThrottleError("The client must wait and try again later.",
                            Convert.ToInt32(waitTime), e);
                    case 401:
                        throw new ExpiredTokenError(e.Message, e);
                }

                response = null;
                return false;
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
            var accessToken = GetAccessToken();
            client.Authenticator = new OAuth2AuthorizationRequestHeaderAuthenticator(accessToken, "Bearer");
            return client;
        }

        private RestRequest BuildRestRequest(String apiPath, Method method)
        {
            var request = new RestRequest(apiPath, method)
            {
                RequestFormat = DataFormat.Json
            };
            return request;
        }

        private String GetAccessToken(Boolean renew = false)
        {
            lock (AccessTokenLock)
            {
                if (_accessToken == null || renew)
                {

                    var uri = new Uri(BaseUrl + TokenPath);
                    var nameValues = new NameValueCollection();
                    nameValues.Add("grant_type", "client_credentials");
                    nameValues.Add("client_id", ClientId);
                    nameValues.Add("client_secret", ClientSecret);
                    var postData = ToQueryString(nameValues);
                    var request = WebRequest.Create(uri);
                    request.Method = "POST";
                    var ascii = new ASCIIEncoding();
                    var postBytes = ascii.GetBytes(postData);
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.ContentLength = postBytes.Length;
                    using (var postStream = request.GetRequestStream())
                    {
                        postStream.Write(postBytes, 0, postBytes.Length);
                        postStream.Flush();
                        postStream.Close();
                    }

                    var response = request.GetResponse();
                    string responseString;

                    using (var dataStream = response.GetResponseStream())
                    {
                        if (dataStream != null)
                        {
                            using (var reader = new StreamReader(dataStream))
                            {
                                responseString = reader.ReadToEnd();
                            }
                        }
                        else
                        {
                            responseString = "";
                        }
                    }

                    var joResponse = JObject.Parse(responseString);
                    _accessToken = joResponse.GetValue("access_token").ToString();
                }

                return _accessToken.Clone().ToString();
            }
        }

        private string ToQueryString(NameValueCollection nvc)
        {
            var array = (from key in nvc.AllKeys
                from value in nvc.GetValues(key)
                select string.Format("{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value)))
                .ToArray();
            return string.Join("&", array);
        }
    }
}