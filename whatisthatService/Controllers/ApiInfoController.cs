using System.ServiceModel.Web;
using System.Web.Http;
using whatisthatService.Core.Classification;
using whatisthatService.DataObjects;

namespace whatisthatService.Controllers
{
    public class ApiInfoController : ApiController
    {
        private readonly SpeciesIdentifier _speciesIdentifier = new SpeciesIdentifier();

        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        public ApiInfo Get()
        {
            const double apiVersion = 0.1;
            var maxImageSize = _speciesIdentifier.GetMaxImageSize();
            var minImageSize = _speciesIdentifier.GetMinImageSize();

            return new ApiInfo(apiVersion, maxImageSize, minImageSize);
        }
    }
}