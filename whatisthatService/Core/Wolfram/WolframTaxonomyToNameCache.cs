using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using whatisthatService.Core.Classification;
using whatisthatService.Core.Utilities.Caching;
using whatisthatService.Core.Wolfram.Response;

namespace whatisthatService.Core.Wolfram
{
    public class WolframTaxonomyToNameCache
    {
        private static readonly String WhatIsThatDbConnString = ConfigurationManager.AppSettings["whatisthatdb_connection"];
        private static readonly GenericMemoryCache<WolframCommonNameData> CommonNameCache = new GenericMemoryCache<WolframCommonNameData>();

        public WolframCommonNameData Get(TaxonomicClassification taxonomy)
        {
            var cacheKey = GenerateCommonNameCacheKey(taxonomy);
            var cachedCommonNameData = CommonNameCache.Get(cacheKey);

            if (cachedCommonNameData != null)
            {
                return cachedCommonNameData;
            }

            var commonNameData = GetCommonNameDataFromDb(taxonomy);

            if (commonNameData != null)
            {
                CommonNameCache.Set(cacheKey, commonNameData);
            }

            return commonNameData;
        }

        private WolframCommonNameData GetCommonNameDataFromDb(TaxonomicClassification taxonomy)
        {
            using (var sqlConnection = new SqlConnection(WhatIsThatDbConnString))
            {
                sqlConnection.Open();
                var cmd = new SqlCommand("SELECT TOP 1 * FROM TaxonomyToName WHERE Kingdom=@Kingdom AND Phylum=@Phylum AND " +
                                         "Class=@Class AND [Order]=@Order AND Family=@Family AND Genus=@Genus AND Species=@Species ")
                {
                    CommandType = CommandType.Text,
                    Connection = sqlConnection
                };

                cmd.Parameters.AddWithValue("@Kingdom", taxonomy.GetKingdom().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Phylum", taxonomy.GetPhylum().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Class", taxonomy.GetClass().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Order", taxonomy.GetOrder().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Family", taxonomy.GetFamily().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Genus", taxonomy.GetGenus().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Species", taxonomy.GetSpecies().Trim().ToLower());

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            var commonName = reader["CommonName"].ToString();
                            return WolframCommonNameData.GetInstance(commonName);
                        }
                    }
                }
            }

            return null;
        }

        public void Set(TaxonomicClassification taxonomy, WolframCommonNameData commonNameData)
        {
            using (var sqlConnection = new SqlConnection(WhatIsThatDbConnString))
            {
                sqlConnection.Open();
                var cmd = new SqlCommand("UPDATE TaxonomyToName " +
                         "SET CommonName=@CommonName " +
                         "WHERE Kingdom=@Kingdom AND Phylum=@Phylum AND Class=@Class AND [Order]=@Order " +
                                         "AND Family=@Family AND Genus=@Genus AND Species=@Species " +
                         "IF @@ROWCOUNT=0 INSERT INTO TaxonomyToName (CommonName, Kingdom, Phylum, Class, [Order], Family, Genus, Species) " +
                         "VALUES (@CommonName, @Kingdom, @Phylum, @Class, @Order, @Family, @Genus, @Species)")
                {
                    CommandType = CommandType.Text,
                    Connection = sqlConnection
                };

                cmd.Parameters.AddWithValue("@CommonName", commonNameData.Name.Trim().ToLower());
                cmd.Parameters.AddWithValue("@Kingdom", taxonomy.GetKingdom().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Phylum", taxonomy.GetPhylum().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Class", taxonomy.GetClass().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Order", taxonomy.GetOrder().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Family", taxonomy.GetFamily().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Genus", taxonomy.GetGenus().Trim().ToLower());
                cmd.Parameters.AddWithValue("@Species", taxonomy.GetSpecies().Trim().ToLower());
                cmd.ExecuteNonQuery();
            }

            var cacheKey = GenerateCommonNameCacheKey(taxonomy);
            CommonNameCache.Set(cacheKey, commonNameData);
        }

        private String GenerateCommonNameCacheKey(TaxonomicClassification taxonomy)
        {
            var keyBuffer = new StringBuilder();
            keyBuffer.Append(taxonomy.GetGenus());
            keyBuffer.Append("_");
            keyBuffer.Append(taxonomy.GetSpecies());
            return keyBuffer.ToString();
        }
    }
}