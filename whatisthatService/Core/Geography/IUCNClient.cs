using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Spatial;
using System.Text;
using System.Threading.Tasks;
using GeoAPI;
using GeoAPI.Geometries;
using NetTopologySuite;
using SharpMap.Data;
using SharpMap.Data.Providers;
using whatisthatService.Core.Classification;
using whatisthatService.Core.Geography.Dto;
using whatisthatService.Core.Utilities.Caching;
using Geometry = NetTopologySuite.Geometries.Geometry;

namespace whatisthatService.Core.Geography
{
    public class IUCNClient
    {
        private static readonly DirectoryInfo GeographyDirectoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory() + "/Geography/IUCN/");
        private static readonly FileInfo MammalsShapeFileInfo = new FileInfo(GeographyDirectoryInfo.FullName + "/Mammals/MAMMALS.shp");
        private static readonly FileInfo AmphibiansShapeFileInfo = new FileInfo(GeographyDirectoryInfo.FullName + "/Amphibians/AMPHIBIANS.shp");
        private static readonly FileInfo ReptilesShapeFileInfo = new FileInfo(GeographyDirectoryInfo.FullName + "/Reptiles/REPTILES.shp");
        private static readonly ImmutableList<String> SupportedTaxonomicClasses = ImmutableList.Create("mammalia", "reptilia", "amphibia");
        private static readonly GenericLongTermCache<GeographicSpeciesListDto> GeographicSpeciesListDataCache = new GenericLongTermCache<GeographicSpeciesListDto>();

        static IUCNClient()
        {
            GeometryServiceProvider.Instance = new NtsGeometryServices();
        }

        public List<TaxonomicClassification> GetLocalSpeciesTaxonomiesList(GeographyPoint coordinates)
        {
            var dtos = GetApplicableSpeciesListDtos(coordinates);
            var classificationsLookup = new Dictionary<String, TaxonomicClassification>();
            //Strangely, humans aren't listed in their data!  We need it for our purposes.
            classificationsLookup.Add("Homo.sapien", TaxonomicClassification.HUMAN);

            foreach (var dto in dtos)
            {
                var classifications = dto.SpeciesInfoDtoList.Select(speciesInfoDto =>
                    TaxonomicClassification.GetInstance(speciesInfoDto.Kingdom, speciesInfoDto.Phylum, speciesInfoDto.Class,
                        speciesInfoDto.Order,
                        speciesInfoDto.Family, speciesInfoDto.Genus, speciesInfoDto.Species)).ToList();

                foreach (var classification in classifications)
                {
                    var key = classification.GetGenus().Trim().ToLower() + "." + classification.GetSpecies().Trim().ToLower();

                    if (!classificationsLookup.ContainsKey(key))
                    {
                        classificationsLookup.Add(key, classification);
                    }
                }
            }

            return classificationsLookup.Values.ToList();
        }

        private List<GeographicSpeciesListDto> GetApplicableSpeciesListDtos(GeographyPoint sourceCoordinates)
        {
            var coordinatesList = GenerateListOfCoordinatesToQuery(sourceCoordinates);
            var speciesLists = new ConcurrentBag<GeographicSpeciesListDto>();
            Parallel.ForEach(coordinatesList, coordinate =>
            {
                var dto = GetGeographicSpeciesListDto(coordinate);
                speciesLists.Add(dto);
            });

            return speciesLists.ToList();
        }

        //Creates a list of coordinates that form a square around the center coordinate, so that rounding for caching/performance purposes doesn't
        //have the adverse effect of cutting out good data.
        private List<GeographyPoint> GenerateListOfCoordinatesToQuery(GeographyPoint coordinates)
        {
            const double step = 0.5;
            var roundedCoordinates = GeographyPoint.Create(Math.Round(coordinates.Latitude, 0), Math.Round(coordinates.Longitude, 0));
            var coordinatesList = new List<GeographyPoint>
            {
                roundedCoordinates,
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude + step, 1),
                    Math.Round(roundedCoordinates.Longitude + step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude + step, 1),
                    Math.Round(roundedCoordinates.Longitude, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude + step, 1),
                    Math.Round(roundedCoordinates.Longitude - step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude - step, 1),
                    Math.Round(roundedCoordinates.Longitude + step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude - step, 1),
                    Math.Round(roundedCoordinates.Longitude, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude - step, 1),
                    Math.Round(roundedCoordinates.Longitude - step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude, 1),
                    Math.Round(roundedCoordinates.Longitude + step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude, 1),
                    Math.Round(roundedCoordinates.Longitude - step, 1))
            };


            return coordinatesList;
        }

        public GeographicSpeciesListDto GetGeographicSpeciesListDto(GeographyPoint coordinates)
        {
            var cacheKey = GenerateSpeciesDistributionDtoCacheKey(coordinates);
            var cachedDto = GeographicSpeciesListDataCache.Get(cacheKey);
            if (cachedDto != null)
            {
                return cachedDto;
            }

            var dto = ReadSpeciesDistributionFromShapeFiles(coordinates);
            GeographicSpeciesListDataCache.Set(cacheKey, dto);
            return dto;
        }

        private String GenerateSpeciesDistributionDtoCacheKey(GeographyPoint coordinates)
        {
            var cacheKeyBuffer = new StringBuilder();
            cacheKeyBuffer.Append(coordinates.Latitude.ToString(CultureInfo.InvariantCulture));
            cacheKeyBuffer.Append(".");
            cacheKeyBuffer.Append(coordinates.Longitude.ToString(CultureInfo.InvariantCulture));
            return cacheKeyBuffer.ToString();
        }



        private GeographicSpeciesListDto ReadSpeciesDistributionFromShapeFiles(GeographyPoint coordinates)
        {

            var speciesInfoDictionary = new Dictionary<String, GeographicSpeciesInfoDto>();


            foreach (var supportedClass in SupportedTaxonomicClasses)
            {
                ShapeFile shapeFile = null;

                try
                {
                    var shapeFileInfo = GetShapeFileInfo(supportedClass);
                    if (shapeFileInfo == null)
                    {
                        continue;
                    }

                    shapeFile = new ShapeFile(shapeFileInfo.FullName);
                    shapeFile.Open();


                    var geometryObject = Geometry.DefaultFactory.CreatePoint(new Coordinate(coordinates.Longitude, coordinates.Latitude));
                    var ds = new FeatureDataSet();

                    //example uses a map image, but this could be a layer generated with code
                    shapeFile.ExecuteIntersectionQuery(geometryObject.Centroid, ds);

                    foreach (var dt in ds.Tables)
                    {
                        foreach (DataRow row in dt.Rows)
                        {
                            var speciesInfoDto = new GeographicSpeciesInfoDto
                            {
                                Kingdom = row.ItemArray[17].ToString(),
                                Phylum = row.ItemArray[18].ToString(),
                                Class = row.ItemArray[19].ToString(),
                                Order = row.ItemArray[20].ToString(),
                                Family = row.ItemArray[21].ToString(),
                                Genus = row.ItemArray[22].ToString(),
                                Species = row.ItemArray[23].ToString(),
                                PresenceCode = Int32.Parse(row.ItemArray[14].ToString())
                            };
                            var key = speciesInfoDto.Species.Trim().ToLower();

                            //Presence code of >= 4 means the species probably doesn't exist in the area ("Possibly extinct")
                            if (!speciesInfoDictionary.ContainsKey(key) && speciesInfoDto.PresenceCode < 4)
                            {
                                speciesInfoDictionary.Add(key, speciesInfoDto);
                            }
                        }
                    }

                }
                finally
                {
                    if (shapeFile != null)
                    {
                        try
                        {
                            shapeFile.Close();
                            shapeFile.Dispose();
                        }
                        catch (Exception)
                        {
                            //do nothing
                        }
                    }
                }

            }

            var geographicSpeciesListDto = new GeographicSpeciesListDto
            {
                Latitude = coordinates.Latitude,
                Longitude = coordinates.Longitude,
                SpeciesInfoDtoList = speciesInfoDictionary.Values.ToList()
            };


            return geographicSpeciesListDto;
        }

        private FileInfo GetShapeFileInfo(String taxonomicClass)
        {
            switch (taxonomicClass)
            {
                case "mammalia":
                    return MammalsShapeFileInfo;
                case "amphibia":
                    return AmphibiansShapeFileInfo;
                case "reptilia":
                    return ReptilesShapeFileInfo;
                default:
                    return null;
            }
        }
    }
}
