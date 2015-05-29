using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using whatisthatService.Core.Utilities.Caching;
using whatisthatService.Core.Wolfram.Response;

namespace whatisthatService.Core.Wolfram
{
    public class WolframTagToTaxonomyCache
    {
        private static readonly String WhatIsThatDbConnString = ConfigurationManager.AppSettings["whatisthatdb_connection"];
        private static readonly GenericMemoryCache<WolframTaxonomyData> TaxonomicDataCache = new GenericMemoryCache<WolframTaxonomyData>();

        public WolframTaxonomyData Get(String tag)
        {
            var cacheKey = GenerateTaxonomicDataCacheKey(tag);
            var cachedTaxonomyData = TaxonomicDataCache.Get(cacheKey);

            if (cachedTaxonomyData != null)
            {
                return cachedTaxonomyData;
            }

            var taxonomyData = GetTaxonomyDataFromDB(tag);

            if (taxonomyData != null)
            {
                TaxonomicDataCache.Set(cacheKey, taxonomyData);
            }

            return taxonomyData;
        }

        private WolframTaxonomyData GetTaxonomyDataFromDB(String tag)
        {
            using (var sqlConnection = new SqlConnection(WhatIsThatDbConnString))
            {
                sqlConnection.Open();
                var cmd = new SqlCommand("SELECT TOP 1 * FROM TagToTaxonomy WHERE Tag=@Tag ")
                {
                    CommandType = CommandType.Text,
                    Connection = sqlConnection
                };

                cmd.Parameters.AddWithValue("@Tag", tag.Trim().ToLower());

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var kingdom = reader["Kingdom"].ToString();
                            var phylum = reader["Phylum"].ToString();
                            var tclass = reader["Class"].ToString();
                            var order = reader["Order"].ToString();
                            var family = reader["Family"].ToString();
                            var genus = reader["Genus"].ToString();
                            var species = reader["Species"].ToString();

                            return WolframTaxonomyData.GetInstance(kingdom, phylum, tclass, order, family, genus, species);
                        }
                    }
                }
            }

            return null;
        }

        public void Set(String tag, WolframTaxonomyData taxonomyData)
        {
            using (var sqlConnection = new SqlConnection(WhatIsThatDbConnString))
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

                cmd.Parameters.AddWithValue("@Tag", tag.Trim().ToLower());
                cmd.Parameters.AddWithValue("@Kingdom", taxonomyData.Kingdom);
                cmd.Parameters.AddWithValue("@Phylum", taxonomyData.Phylum);
                cmd.Parameters.AddWithValue("@Class", taxonomyData.Class);
                cmd.Parameters.AddWithValue("@Order", taxonomyData.Order);
                cmd.Parameters.AddWithValue("@Family", taxonomyData.Family);
                cmd.Parameters.AddWithValue("@Genus", taxonomyData.Genus);
                cmd.Parameters.AddWithValue("@Species", taxonomyData.Species);
                cmd.ExecuteNonQuery();
            }

            var cacheKey = GenerateTaxonomicDataCacheKey(tag);
            TaxonomicDataCache.Set(cacheKey, taxonomyData);
        }

        private String GenerateTaxonomicDataCacheKey(String tag)
        {
            return tag.Trim().ToLower();
        }
    }
}