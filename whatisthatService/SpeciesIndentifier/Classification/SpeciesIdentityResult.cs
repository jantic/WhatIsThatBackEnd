using System.Drawing;

namespace whatisthatService.SpeciesIndentifier.Classification
{
    public class SpeciesIdentityResult
    {
        private readonly Image _sourceImage;
        private readonly Rectangle _cropArea;
        private readonly SpeciesInfo _likelySpeciesInfo;

        public static SpeciesIdentityResult NULL = new SpeciesIdentityResult(new Bitmap(1,1), new Rectangle(), SpeciesInfo.NULL);

        public static SpeciesIdentityResult GetInstance(Image sourceImage, Rectangle cropArea, SpeciesInfo likelySpeciesInfo)
        {
            return likelySpeciesInfo == SpeciesInfo.NULL ? NULL : new SpeciesIdentityResult(sourceImage, cropArea, likelySpeciesInfo);
        }

        private SpeciesIdentityResult(Image sourceImage, Rectangle cropArea, SpeciesInfo likelySpeciesInfo)
        {
            _sourceImage = sourceImage;
            _cropArea = cropArea;
            _likelySpeciesInfo = likelySpeciesInfo;
        }

        public Image SourceImage
        {
            get { return _sourceImage; }
        }

        public Rectangle CropArea
        {
            get { return _cropArea; }
        }

        public SpeciesInfo LikelySpeciesInfo
        {
            get { return _likelySpeciesInfo; }
        }
    }
}
