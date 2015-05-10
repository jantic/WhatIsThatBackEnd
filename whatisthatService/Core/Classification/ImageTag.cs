using System;

namespace whatisthatService.Core.Classification
{
    public class ImageTag
    {
        private readonly String _name;
        private readonly Double _probability;

        public ImageTag(String name, Double probability)
        {
            _name = name;
            _probability = probability;
        }

        public String Name
        {
            get { return _name; }
        }

        public Double Probability
        {
            get { return _probability; }
        }
    }
}