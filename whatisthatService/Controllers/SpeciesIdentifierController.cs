using System;
using System.Collections.Generic;
using System.Spatial;
using System.Web.Http;
using whatisthatService.Core.Classification;
using whatisthatService.Core.Utilities.ImageProcessing;
using whatisthatService.DataObjects;

namespace whatisthatService.Controllers
{
    public class SpeciesIdentifierController : ApiController
    {
        private readonly SpeciesIdentifier _speciesIdentifier = new SpeciesIdentifier();
        // GET api/GetSpeciesCandidates
        public List<SpeciesCandidate> GetSpeciesCandidates(Byte[] imageBytes, double latitude, double longitude, Boolean multisample)
        {
            if (imageBytes != null && imageBytes.Length > 0)
            {
                var image = ImageConversion.ByteArrayToImage(imageBytes);
                var geographyPoint = GeographyPoint.Create(latitude, longitude);
                var speciesIdentityResult = _speciesIdentifier.GetMostLikelyIdentity(image, geographyPoint, true, multisample);
                var speciesInfo = speciesIdentityResult.LikelySpeciesInfo;

                var serializedResult = new List<SpeciesCandidate>
                {
                    new SpeciesCandidate(speciesInfo.GetName(),
                        speciesInfo.Taxonomy.GetGenus() + " " + speciesInfo.Taxonomy.GetSpecies(),
                        speciesInfo.GetProbability())
                };
                return serializedResult;
            }

            const string message = "Valid image must be supplied.";
            throw new ApplicationException(message);
        }
    }
}