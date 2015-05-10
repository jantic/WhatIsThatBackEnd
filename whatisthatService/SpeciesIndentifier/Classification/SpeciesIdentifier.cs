using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Spatial;
using System.Threading.Tasks;
using whatisthatService.SpeciesIndentifier.Clarifai;
using whatisthatService.SpeciesIndentifier.Classification.Exceptions;
using whatisthatService.SpeciesIndentifier.Utilities.ImageProcessing;
using whatisthatService.SpeciesIndentifier.Wolfram;

namespace whatisthatService.SpeciesIndentifier.Classification
{
    public class SpeciesIdentifier
    {
        //Minimum viable image size factor on api size minimum.  Enforced to ensure dependability.
        private const Double MinImageSizeMultiplier = 0.10;
        private readonly ClarifaiClient _clarifaiClient = new ClarifaiClient();
        private readonly WolframClient _wolframClient = new WolframClient();
        private readonly TagsFilterFactory _tagsFilterFactory = new TagsFilterFactory();


        public SpeciesIdentityResult GetMostLikelyIdentity(Image sourceImage, GeographyPoint coordinates, Boolean geoContextMode, Rectangle mainCropArea, Boolean multisample)
        {
            var formattedSourceImage = ImageConversion.ConvertTo24BitColorBitmap(sourceImage);
            //ValidateImage(formattedSourceImage);
            ValidateCropArea(mainCropArea);
            var candidates = new ConcurrentBag<SpeciesIdentityResult>();
            var cropAreas = GetCrops(mainCropArea, multisample);
            var sourceImageClones = new ConcurrentStack<Bitmap>(cropAreas.Select(cropArea => (Bitmap)formattedSourceImage.Clone()).ToList());

            Parallel.ForEach(cropAreas, cropArea =>
            {
                Bitmap imageClone;
                if (sourceImageClones.TryPop(out imageClone))
                {
                    var speciesInfo = GetTopCandidateSpeciesInfoForImage(imageClone, coordinates, geoContextMode, cropArea);
                    var candidate = SpeciesIdentityResult.GetInstance(imageClone, cropArea, speciesInfo);
                    candidates.Add(candidate);
                }
            });

            var candidatesList = candidates.ToList();
            if (!multisample) return candidatesList.FirstOrDefault() ?? SpeciesIdentityResult.NULL;
            var metaFilterFactory = new MetaFilterFactory();
            var metaFilters = metaFilterFactory.GetOrderedFilters();
            candidatesList = metaFilters.Aggregate(candidatesList,
                (current, metaFilter) => metaFilter.Filter(current));
            return candidatesList.FirstOrDefault() ?? SpeciesIdentityResult.NULL;
        }

        //(If multisample == true) Process image split in half horizontally, then vertically, then as a whole image.  This should
        //cut down on false positives because real hits will have heads that should be recognizable after the split, generally
        private List<Rectangle> GetCrops(Rectangle mainCropArea, Boolean multisample)
        {
            var splitCropAreas = new List<Rectangle> {mainCropArea};
            //Full crop area

            if (!multisample) return splitCropAreas;

            var halfHeight = Convert.ToInt32(mainCropArea.Height/2);
            //Horizontal split
            splitCropAreas.Add(new Rectangle(mainCropArea.X, mainCropArea.Y, mainCropArea.Width, halfHeight));
            splitCropAreas.Add(new Rectangle(mainCropArea.X, mainCropArea.Y + halfHeight, mainCropArea.Width, halfHeight));

            var halfWidth = Convert.ToInt32(mainCropArea.Width/2);
            //Vertical split
            splitCropAreas.Add(new Rectangle(mainCropArea.X, mainCropArea.Y, halfWidth, mainCropArea.Height));
            splitCropAreas.Add(new Rectangle(mainCropArea.X + halfWidth, mainCropArea.Y, halfWidth, mainCropArea.Height));

            //Quadrant split
            //TODO:  Disabling for now because this doesn't seem to help accuracy, believe it or not.
            /*splitCropAreas.Add(new Rectangle(mainCropArea.X, mainCropArea.Y, halfWidth, halfHeight));
            splitCropAreas.Add(new Rectangle(mainCropArea.X + halfWidth, mainCropArea.Y, halfWidth, halfHeight));
            splitCropAreas.Add(new Rectangle(mainCropArea.X, mainCropArea.Y + halfHeight, halfWidth, halfHeight));
            splitCropAreas.Add(new Rectangle(mainCropArea.X + halfWidth, mainCropArea.Y + halfHeight, halfWidth, halfHeight));*/


            return splitCropAreas;
        }

        private SpeciesInfo GetTopCandidateSpeciesInfoForImage(Bitmap sourceImage, GeographyPoint coordinates, Boolean geoContextMode, Rectangle cropArea)
        {
            var candidateSpeciesList = GetListOfCandidateIdentities(sourceImage, cropArea);

            var speciesFilterFactory = new SpeciesFilterFactory(geoContextMode); 
            var speciesFilters = speciesFilterFactory.GetOrderedFilters();
            var speciesInfos = speciesFilters.Aggregate(candidateSpeciesList,
                (current, filter) => filter.Filter(current, coordinates));

            return speciesInfos.Count > 0 ? speciesInfos.First() : SpeciesInfo.NULL;
        }

        public Int32 GetMinImageWidth()
        {
            return Convert.ToInt32(Math.Ceiling(MinImageSizeMultiplier*_clarifaiClient.GetApiInfo().MinImageSize));
        }

        public Int32 GetMinImageHeight()
        {
            return Convert.ToInt32(Math.Ceiling(MinImageSizeMultiplier * _clarifaiClient.GetApiInfo().MinImageSize));
        }

        /*private void ValidateImage(Image image)
        {
            if (image.Width >= GetMinImageWidth() && image.Height >= GetMinImageHeight()) return;
            var message = "Image is too small!  Min width is " + GetMinImageWidth() + " and min height is " +
                          GetMinImageHeight();
            throw new ApplicationException(message);
        }*/

        private void ValidateCropArea(Rectangle cropArea)
        {
            if (cropArea.Width >= GetMinImageWidth() && cropArea.Height >= GetMinImageHeight()) return;
            var message = "Crop area is too small!  Min width is " + GetMinImageWidth() + " and min height is " +
                          GetMinImageHeight();
            throw new ApplicationException(message);
        }

        //For threadsafe operations- assumes we already have 100% control on an image clone at this point
        private Bitmap GetImageClone(Bitmap image)
        {
            lock(image)
            {
                return (Bitmap)image.Clone();
            }
        }

        private List<SpeciesInfo> GetListOfCandidateIdentities(Bitmap image, Rectangle cropArea)
        {
            using (var imageClone = GetImageClone(image))
            {
                var subImage = ImageConversion.GetImageCrop(imageClone, cropArea);
                return GetListOfCandidateIdentities(subImage);
            }
        }


        private List<SpeciesInfo> GetListOfCandidateIdentities(Bitmap image)
        {
            var tagsResult = _clarifaiClient.GetTagsInfo(image);


            if (tagsResult != null)
            {
                //If not an animal species/genus/order/etc, tag result will be NULL
                var candidateBag = new ConcurrentBag<SpeciesInfo>();
                var tagsFilters = _tagsFilterFactory.GetOrderedFilters();
                var filteredTags = tagsFilters.Aggregate(tagsResult.ImageTags, (current, filter) => filter.Filter(current));

                Parallel.ForEach(filteredTags, tag =>
                {
                    var taxonomy = _wolframClient.GetTaxonomyData(tag.Name);
                    if (!String.IsNullOrEmpty(taxonomy.GetKingdom()))
                    {
                        var speciesName = _wolframClient.GetCommonNameFromScientific(taxonomy);
                        var finalName = String.IsNullOrEmpty(speciesName) ? tag.Name : speciesName;
                        candidateBag.Add(SpeciesInfo.GetInstance(taxonomy, finalName, tag.Name, tag.Probability));
                    }
                });

                return candidateBag.ToList();
            }

            const string message = "Failed to identify image- Did not get result back for image tagging.";
            throw new NoClientResponseError(message);
        }
    }
}