using System.Drawing;

namespace whatisthatService.Core.Utilities.ImageProcessing
{
    public class BoundingRectangle
    {
        private readonly Point _leftTop;
        private readonly Point _rightBottom;

        public BoundingRectangle(Point leftTop, Point rightBottom)
        {
            _leftTop = leftTop;
            _rightBottom = rightBottom;
        }

        public Point LeftTop
        {
            get { return _leftTop; }
        }

        public Point RightBottom
        {
            get { return _rightBottom; }
        }
    }
}
