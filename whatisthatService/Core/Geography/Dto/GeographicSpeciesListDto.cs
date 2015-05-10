using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace whatisthatService.Core.Geography.Dto
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [Serializable]
    public class GeographicSpeciesListDto
    {
        public static GeographicSpeciesListDto NULL = new GeographicSpeciesListDto();

        public Double Latitude { set; get; }
        public Double Longitude { get; set; }

        public List<GeographicSpeciesInfoDto> SpeciesInfoDtoList { set; get; }
    }
}
