using System;

namespace whatisthatService.Core.Geography.Dto
{
    public class GeographicSpeciesInfoDto
    {
        public String Kingdom { get; set; }
        public String Phylum { get; set; }
        public String Class { get; set; }
        public String Order { get; set; }
        public String Family { get; set; }
        public String Genus { get; set; }
        public String Species { get; set; }
        public Int32 PresenceCode { get; set; }
    }
}
