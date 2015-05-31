using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Spatial;
using System.Text;
using System.Web;
using System.Web.Http;
using AForge.Imaging;
using whatisthatService.Core.Classification;
using whatisthatService.Core.Utilities.ImageProcessing;
using whatisthatService.DataObjects;

namespace whatisthatService.Controllers
{
    public class IdentifySpeciesController : ApiController
    {
        private readonly SpeciesIdentifier _speciesIdentifier = new SpeciesIdentifier();
        private const String FailureStatus = "FAILURE";
        private const String SuccessStatus = "SUCCESS";

        [WebInvoke(ResponseFormat = WebMessageFormat.Json, RequestFormat = WebMessageFormat.Json)]
        public SpeciesCandidatesResult Post(Double latitude, Double longitude, Boolean multisample, byte[] imageBytes)
        {
            if (imageBytes == null)
            {
                return new SpeciesCandidatesResult(new List<SpeciesCandidate>(), FailureStatus, "Valid image must be supplied.");
            }

            try
            {
                var image = ImageConversion.ByteArrayToImage(imageBytes);
                var geographyPoint = GeographyPoint.Create(latitude, longitude);
                var speciesIdentityResult = _speciesIdentifier.GetMostLikelyIdentity(image, geographyPoint, true, multisample);
                var speciesInfo = speciesIdentityResult.LikelySpeciesInfo;

                var speciesCandidates = new List<SpeciesCandidate>
                {
                    new SpeciesCandidate(speciesInfo.GetName(),
                        speciesInfo.Taxonomy.GetGenus() + " " + speciesInfo.Taxonomy.GetSpecies(),
                        speciesInfo.GetProbability())
                };
                return new SpeciesCandidatesResult(speciesCandidates, SuccessStatus, "");
            }
            catch (Exception e)
            {
                var message = "Failure while processing request: " + e.Message;
                return new SpeciesCandidatesResult(new List<SpeciesCandidate>(), FailureStatus, message);
            }

        }

        private static byte[] GetImageByteArrayFromStream(Stream fileContents)
        {
            var buffer = new byte[32768];
            using (var ms = new MemoryStream())
            {
                int bytesRead;

                do
                {
                    bytesRead = fileContents.Read(buffer, 0, buffer.Length);
                    ms.Write(buffer, 0, bytesRead);
                } 
                while (bytesRead > 0);
                return ms.ToArray();
            }


        }
    }
}