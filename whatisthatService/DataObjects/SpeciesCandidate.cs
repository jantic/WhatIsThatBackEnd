using System;
using System.Runtime.Serialization;
using System.ServiceModel.Web;

namespace whatisthatService.DataObjects
{
    [DataContract]
    public class SpeciesCandidate
    {

        public SpeciesCandidate(String commonName, String scientificName , Double confidence)
        {
            CommonName = commonName;
            ScientificName = scientificName;
            Confidence = confidence;
        }

        [DataMember] 
        public string CommonName { get; set; }

        [DataMember] 
        public string ScientificName { get; set; }

        [DataMember] 
        public double Confidence { get; set; }
    }
}