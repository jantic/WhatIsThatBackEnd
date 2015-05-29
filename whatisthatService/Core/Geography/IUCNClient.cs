using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Spatial;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using GeoAPI;
using NetTopologySuite;
using whatisthatService.Core.Classification;
using whatisthatService.Core.Geography.Dto;
using whatisthatService.Core.Utilities.Caching;

namespace whatisthatService.Core.Geography
{
    public class IUCNClient
    {
        private static readonly String WhatIsThatDbConnString = WebConfigurationManager.AppSettings["whatisthatdb_connection"];
        private static readonly GenericMemoryCache<GeographicSpeciesListDto> GeographicSpeciesListDataCache = new GenericMemoryCache<GeographicSpeciesListDto>();

        static IUCNClient()
        {
            GeometryServiceProvider.Instance = new NtsGeometryServices();
        }

        public List<TaxonomicClassification> GetLocalSpeciesTaxonomiesList(GeographyPoint coordinates)
        {
            var dtos = GetApplicableSpeciesListDtos(coordinates);
            var classificationsLookup = new Dictionary<String, TaxonomicClassification>
            {
                {"Homo.sapien", TaxonomicClassification.HUMAN},
            };
            //Strangely, humans aren't listed in their data!  We need it for our purposes.

            foreach (var dto in dtos)
            {
                var classifications = dto.SpeciesInfoDtoList.Select(speciesInfoDto =>
                    TaxonomicClassification.GetInstance(speciesInfoDto.Kingdom, speciesInfoDto.Phylum, speciesInfoDto.Class, speciesInfoDto.Order,
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
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude + step, 1), Math.Round(roundedCoordinates.Longitude + step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude + step, 1), Math.Round(roundedCoordinates.Longitude, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude + step, 1), Math.Round(roundedCoordinates.Longitude - step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude - step, 1), Math.Round(roundedCoordinates.Longitude + step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude - step, 1), Math.Round(roundedCoordinates.Longitude, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude - step, 1), Math.Round(roundedCoordinates.Longitude - step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude, 1), Math.Round(roundedCoordinates.Longitude + step, 1)),
                GeographyPoint.Create(Math.Round(roundedCoordinates.Latitude, 1), Math.Round(roundedCoordinates.Longitude - step, 1))
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

            var dto = ReadSpeciesDistributionFromDatabase(coordinates);
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



        private GeographicSpeciesListDto ReadSpeciesDistributionFromDatabase(GeographyPoint point)
        {

            var speciesInfoDictionary = new Dictionary<String, GeographicSpeciesInfoDto>();

            using (var sqlConnection = new SqlConnection(WhatIsThatDbConnString))
            {
                sqlConnection.Open();

                var cmd = new SqlCommand("SELECT * FROM SpeciesGeography WHERE LatitudeX10 = @LatitudeX10 AND LongitudeX10 = @LongitudeX10")
                {
                    CommandType = CommandType.Text,
                    Connection = sqlConnection
                };

                cmd.Parameters.AddWithValue("@LatitudeX10", Math.Round(point.Latitude * 10, 0));
                cmd.Parameters.AddWithValue("@LongitudeX10", Math.Round(point.Longitude * 10, 0));

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var speciesInfoDto = new GeographicSpeciesInfoDto
                            {
                                Kingdom = reader["Kingdom"].ToString(),
                                Phylum = reader["Phylum"].ToString(),
                                Class = reader["Class"].ToString(),
                                Order = reader["Order"].ToString(),
                                Family = reader["Family"].ToString(),
                                Genus = reader["Genus"].ToString(),
                                Species = reader["Species"].ToString(),
                                PresenceCode = 1
                                //TODO:  This may or may not be relevant for our purposes.  For now, assuming not.
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
            }

            var geographicSpeciesListDto = new GeographicSpeciesListDto
            {
                Latitude = point.Latitude,
                Longitude = point.Longitude,
                SpeciesInfoDtoList = speciesInfoDictionary.Values.ToList()
            };


            return geographicSpeciesListDto;
        }


    }
}
