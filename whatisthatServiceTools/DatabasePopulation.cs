using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Spatial;
using System.Xml.Serialization;
using GeoAPI;
using GeoAPI.Geometries;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpMap.Data.Providers;
using whatisthatService.Core.Geography.Dto;
using NetTopologySuite;
using SharpMap.Data;
using whatisthatService.Core.Wolfram.Response.Dto;
using Geometry = NetTopologySuite.Geometries.Geometry;
using whatisthatService.Core.Wolfram.Response;

namespace whatisthatServiceTools
{
    [TestClass]
    public class DatabasePopulation
    {
        private static readonly DirectoryInfo GeographyDirectoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory() + "/Geography/IUCN/");
        private static readonly FileInfo MammalsShapeFileInfo = new FileInfo(GeographyDirectoryInfo.FullName + "/Mammals/MAMMALS.shp");
        private static readonly FileInfo AmphibiansShapeFileInfo = new FileInfo(GeographyDirectoryInfo.FullName + "/Amphibians/AMPHIBIANS.shp");
        private static readonly FileInfo ReptilesShapeFileInfo = new FileInfo(GeographyDirectoryInfo.FullName + "/Reptiles/REPTILES.shp");
        //private static readonly List<String> SupportedTaxonomicClasses = new List<String>(new[] { "mammalia", "reptilia", "amphibia" });
        private static readonly List<String> SupportedTaxonomicClasses = new List<String>(new[] { "mammalia"});

        //Purpose:  To prepopulate a database table with IUCN data on geographic distribution of species, in 0.5 degree latitude/longitude intervals 
        //around the world.  The source shape files take a very long time to process, so this is effectively a caching of the end result to make
        //the use of this data practical.
        [TestMethod]
        public void PopupulateSpeciesGeographyDb()
        {
            try
            {            
                GeometryServiceProvider.Instance = new NtsGeometryServices();

                foreach (var taxonomicClass in SupportedTaxonomicClasses)
                {
                    UpdateSpeciesGeographyForTaxonomicClass(taxonomicClass);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message, e);
            }
        }

        //Purpose:  To prepopulate a database table with Wolfram Language image tag to species taxonomy lookups (a lot of those tags will not actually be a species).  These
        //calls are very expensive and slow, so this is my way of making sure I don't have to run the ones I've already generated in the prototype again!  
        [TestMethod]
        public void PopulateImageTagToTaxonomicDataDb()
        {
            var cacheDirectoryInfo = new DirectoryInfo(Directory.GetCurrentDirectory() + "/WolframCache/TaxonomicData");
            var cacheFilesList = cacheDirectoryInfo.GetFiles("*.xml");

            foreach (var fileInfo in cacheFilesList)
            {
                var responseDto = ReadFromXmlFile<WolframResponseDto>(fileInfo.FullName);
                var taxonomyData = WolframTaxonomyData.GetInstance(responseDto);
                var tagName = fileInfo.Name.Replace("WolframResponseDto_", "").Replace(".xml", "");
                UpdateDatabaseWithTagToTaxonomicInfo(tagName, taxonomyData);
            }
        }


        /************************************************************************************************************************/
        /*****PRIVATE METHODS****************************************************************************************************/
        /************************************************************************************************************************/

        private void UpdateSpeciesGeographyForTaxonomicClass(String taxonomicClass)
        {
            var shapeFileInfo = GetShapeFileInfo(taxonomicClass);
            using (var shapeFile = new ShapeFile(shapeFileInfo.FullName))
            {
                shapeFile.Open();
                const double minLatitude = -3.5;
                const double maxLatitude = 0.0;
                const double maxLongitude = 180.0;
                const double minLongitude = -180.0;
                const double interval = 0.5;

                for (var latitude = minLatitude; latitude < maxLatitude; Math.Round(latitude = latitude + interval, 1))
                {
                    for (var longitude = minLongitude; longitude < maxLongitude; Math.Round(longitude = longitude + interval, 1))
                    {
                        var point = GeographyPoint.Create(latitude, longitude);
                        AttemptUpdate:

                        try
                        {
                            UpdateSpeciesGeographyTableIfNeeded(shapeFile, point, taxonomicClass);
                        }
                        catch (Exception e)
                        {
                            Trace.TraceError(e.Message, e);
                            goto AttemptUpdate;
                        }
                    }
                }
            }
        }

        private void UpdateSpeciesGeographyTableIfNeeded(ShapeFile shapeFile, GeographyPoint point, String taxonomicClass)
        {
            var whatIsThatDbConnString = ConfigurationManager.AppSettings["whatisthatdb_connection"];

            using (var sqlConnection = new SqlConnection(whatIsThatDbConnString))
            {
                sqlConnection.Open();

                if (!NeedsInsert(point, sqlConnection, taxonomicClass))
                {
                    return;
                }

                var speciesDistroDto = ReadSpeciesDistributionFromShapeFiles(shapeFile, point);

                foreach (var speciesInfoDto in speciesDistroDto.SpeciesInfoDtoList)
                {
                    WriteSpeciesInfoToDatabase(point, speciesInfoDto, sqlConnection);
                }
            } 
        }

        private Boolean NeedsInsert(GeographyPoint point, SqlConnection sqlConnection, String taxonomicClass)
        {
            var cmd = new SqlCommand("SELECT COUNT(*) FROM SpeciesGeography " +
                   " WHERE LatitudeX10 = @LatitudeX10 AND LongitudeX10 = @LongitudeX10 AND Class = @Class")
            {
                CommandType = CommandType.Text,
                Connection = sqlConnection
            };

            cmd.Parameters.AddWithValue("@LatitudeX10", Math.Round(point.Latitude * 10, 0));
            cmd.Parameters.AddWithValue("@LongitudeX10", Math.Round(point.Longitude * 10, 0));
            cmd.Parameters.AddWithValue("@Class", taxonomicClass.ToUpper());
            var result = cmd.ExecuteScalar();

            if (result == null)
            {
                return true;
            }

            return (Convert.ToInt64(result.ToString()) == 0);
        }

        private void WriteSpeciesInfoToDatabase(GeographyPoint point, GeographicSpeciesInfoDto speciesInfoDto, SqlConnection sqlConnection)
        {
            var cmd = new SqlCommand("UPDATE SpeciesGeography " +
                                     "SET LatitudeX10=@LatitudeX10, LongitudeX10=@LongitudeX10, Kingdom=@Kingdom, Phylum=@Phylum, Class=@Class, [Order]=@Order, Family=@Family, " +
                                     "Genus=@Genus, Species=@Species " +
                                     "WHERE LatitudeX10=@LatitudeX10 AND LongitudeX10=@LongitudeX10 AND Kingdom=@Kingdom AND Phylum=@Phylum AND Class=@Class AND [Order]=@Order AND Family=@Family AND " +
                                     "Genus=@Genus AND Species=@Species " +
                                     "IF @@ROWCOUNT=0 INSERT INTO SpeciesGeography (LatitudeX10, LongitudeX10, Kingdom, Phylum, Class, [Order], Family, Genus, Species) " +
                                     "VALUES (@LatitudeX10, @LongitudeX10, @Kingdom, @Phylum, @Class, @Order, @Family, @Genus, @Species)")
            {
                CommandType = CommandType.Text,
                Connection = sqlConnection
            };

            cmd.Parameters.AddWithValue("@LatitudeX10", Math.Round(point.Latitude * 10, 0));
            cmd.Parameters.AddWithValue("@LongitudeX10", Math.Round(point.Longitude * 10, 0));
            cmd.Parameters.AddWithValue("@Kingdom", speciesInfoDto.Kingdom);
            cmd.Parameters.AddWithValue("@Phylum", speciesInfoDto.Phylum);
            cmd.Parameters.AddWithValue("@Class", speciesInfoDto.Class);
            cmd.Parameters.AddWithValue("@Order", speciesInfoDto.Order);
            cmd.Parameters.AddWithValue("@Family", speciesInfoDto.Family);
            cmd.Parameters.AddWithValue("@Genus", speciesInfoDto.Genus);
            cmd.Parameters.AddWithValue("@Species", speciesInfoDto.Species);
            cmd.ExecuteNonQuery();

        }

        private GeographicSpeciesListDto ReadSpeciesDistributionFromShapeFiles(ShapeFile shapeFile, GeographyPoint coordinates)
        {

            var speciesInfoDictionary = new Dictionary<String, GeographicSpeciesInfoDto>();
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

        private T ReadFromXmlFile<T>(String fileName)
        {
            if (!File.Exists(fileName))
            {
                return default(T);
            }

            var reader = new XmlSerializer(typeof(T));

            using (var file = new StreamReader(fileName))
            {
                return (T)reader.Deserialize(file);
            }
        }


        private void UpdateDatabaseWithTagToTaxonomicInfo(String tagName, WolframTaxonomyData taxonomyData)
        {
            var whatIsThatDbConnString = ConfigurationManager.AppSettings["whatisthatdb_connection"];

            using (var sqlConnection = new SqlConnection(whatIsThatDbConnString))
            {
                sqlConnection.Open();
                var cmd = new SqlCommand("UPDATE TagToTaxonomy " +
                         "SET Kingdom=@Kingdom, Phylum=@Phylum, Class=@Class, [Order]=@Order, Family=@Family, " +
                         "Genus=@Genus, Species=@Species " +
                         "WHERE Tag=@Tag " +
                         "IF @@ROWCOUNT=0 INSERT INTO TagToTaxonomy (Tag, Kingdom, Phylum, Class, [Order], Family, Genus, Species) " +
                         "VALUES (@Tag, @Kingdom, @Phylum, @Class, @Order, @Family, @Genus, @Species)")
                {
                    CommandType = CommandType.Text,
                    Connection = sqlConnection
                };

                cmd.Parameters.AddWithValue("@Tag", tagName.Trim().ToLower());
                cmd.Parameters.AddWithValue("@Kingdom", taxonomyData.Kingdom);
                cmd.Parameters.AddWithValue("@Phylum", taxonomyData.Phylum);
                cmd.Parameters.AddWithValue("@Class", taxonomyData.Class);
                cmd.Parameters.AddWithValue("@Order", taxonomyData.Order);
                cmd.Parameters.AddWithValue("@Family", taxonomyData.Family);
                cmd.Parameters.AddWithValue("@Genus", taxonomyData.Genus);
                cmd.Parameters.AddWithValue("@Species", taxonomyData.Species);
                cmd.ExecuteNonQuery();
            }
        }

    }
}
