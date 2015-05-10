using System;

namespace whatisthatService.DataObjects
{
    public class SpeciesCandidate
    {
        public SpeciesCandidate(String commonName, String scientificName , Double confidence)
        {
            CommonName = commonName;
            ScientificName = scientificName;
            Confidence = confidence;
        }

        public string CommonName { get; set; }
        public string ScientificName { get; set; }
        public double Confidence { get; set; }
    }
}