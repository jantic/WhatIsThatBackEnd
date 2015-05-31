using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace whatisthatService.DataObjects
{
    public class SpeciesCandidatesResult
    {
        public SpeciesCandidatesResult(List<SpeciesCandidate> speciesCandidates, String status, String message)
        {
            SpeciesCandidates = speciesCandidates;
            Status = status;
            Message = message;
        }

        [DataMember]
        public List<SpeciesCandidate> SpeciesCandidates { get; set; }

        [DataMember]
        public String Status { get; set; }

        [DataMember]
        public String Message { get; set; }
    }
}