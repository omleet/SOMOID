using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.InteropServices;
using System.Web;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using SOMOID.Models;

namespace SOMOID.Helpers
{
    public class SQLHelper
    {
        private string connection = SOMOID.Properties.Settings.Default.ConnectionStr;

        public List<string> GetAllApplications()
        {
            var paths = new List<string>();
            using (var conn = new SqlConnection(connection))
            {
                using (
                    var cmd = new SqlCommand(
                        "SELECT [resource-name] FROM [application] ORDER BY [creation-datetime]",
                        conn
                    )
                )
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string appName = (string)reader["resource-name"];
                            paths.Add($"/api/somiod/{appName}");
                        }
                    }
                }
            }
            return paths;
        }

        public List<string> GetAllContainers(string appName)
        {
            var containerPaths = new List<string>();
            using (var conn = new SqlConnection(connection))
            {
                using (
                    var cmd = new SqlCommand(
                        @"
                        SELECT c.[resource-name]
                        FROM [container] c
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                        ORDER BY c.[creation-datetime]",
                        conn
                    )
                )
                {
                    cmd.Parameters.AddWithValue("@appName", appName);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string contName = (string)reader["resource-name"];
                            containerPaths.Add($"/api/somiod/{appName}/{contName}");
                        }
                    }
                }
            }
            return containerPaths;
        }

        public List<string> GetAllContentInstances(string appName)
        {
            var paths = new List<string>();
            using (var conn = new SqlConnection(connection))
            {
                using (
                    var cmd = new SqlCommand(
                        @"
                        SELECT a.[resource-name] AS appName,
                               c.[resource-name] AS contName,
                               ci.[resource-name] AS ciName
                        FROM [content-instance] ci
                        JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                        ORDER BY a.[resource-name], c.[resource-name], ci.[creation-datetime]",
                        conn
                    )
                )
                {
                    cmd.Parameters.AddWithValue("@appName", appName);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string aName = (string)reader["appName"];
                            string cName = (string)reader["contName"];
                            string ciName = (string)reader["ciName"];
                            paths.Add($"/api/somiod/{aName}/{cName}/{ciName}");
                        }
                    }
                }
            }
            return paths;
        }

        public List<string> GetAllSubscriptions(string appName, string containerName)
        {
            var subscriptionPaths = new List<string>();
            using (var conn = new SqlConnection(connection))
            {
                using (
                    var cmd = new SqlCommand(
                        @"
                    SELECT s.[resource-name]
                    FROM [subscription] s
                    JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                    JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                    WHERE a.[resource-name] = @appName
                    AND c.[resource-name] = @containerName
                    ORDER BY s.[creation-datetime]",
                        conn
                    )
                )
                {
                    cmd.Parameters.AddWithValue("@appName", appName);
                    cmd.Parameters.AddWithValue("@containerName", containerName);

                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string subName = (string)reader["resource-name"];
                            subscriptionPaths.Add(
                                $"/api/somiod/{appName}/{containerName}/subs/{subName}"
                            );
                        }
                    }
                }
            }
            return subscriptionPaths;
        }

        public Subscription GetSubscriptionByAppName(string appName, string containerName, string subName)
        {

            using (var conn = new SqlConnection(connection))
            {
                using (var cmd = new SqlCommand(
                    @"
            SELECT s.[resource-name],
                   s.[creation-datetime],
                   s.[container-resource-name],
                   s.[res-type],
                   s.[evt],
                   s.[endpoint]
            FROM [subscription] s
            JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
              AND c.[resource-name] = @containerName
              AND s.[resource-name] = @subName",
                    conn))
                {
                    cmd.Parameters.AddWithValue("@appName", appName);
                    cmd.Parameters.AddWithValue("@containerName", containerName);
                    cmd.Parameters.AddWithValue("@subName", subName);

                    conn.Open();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Subscription sub=  new Subscription
                            {
                                ResourceName = (string)reader["resource-name"],
                                CreationDatetime = (DateTime)reader["creation-datetime"],
                                ContainerResourceName = (string)reader["container-resource-name"],
                                ResType = (string)reader["res-type"],
                                Evt = (int)reader["evt"],
                                Endpoint = (string)reader["endpoint"]
                            };
                            return sub;
                            
                        }
                    }
                }
            }
            return null;

        }


        public int CheckIfSubscriptionParentExists(string appName, string containerName)
        {
            int containerCount = 0;
            string sqlCheckParent =
               @"
                SELECT COUNT(*)
                FROM [container] c 
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @applicationName 
                AND c.[resource-name] = @containerName";
            
            using (var conn = new SqlConnection(connection))
            {
                using (conn)
                {
                    conn.Open();
                    var cmdCheckParent = new SqlCommand(sqlCheckParent, conn);
                    cmdCheckParent.Parameters.AddWithValue("@applicationName", appName);
                    cmdCheckParent.Parameters.AddWithValue("@containerName", containerName);

                    containerCount = (int)cmdCheckParent.ExecuteScalar();
                }
            }
            return containerCount;
        }
        
        public int CheckIfSubscriptionAlreadyExists(string subName, string containerName)
        {
            int subCount = 0;
            string sqlCheckDuplicate =
                @"
                SELECT COUNT(*)
                FROM [subscription]
                WHERE [resource-name] = @subName
                AND [container-resource-name] = @containerName";

            using (var conn = new SqlConnection(connection))
            {
                using (conn)
                {
                    conn.Open();
                    var cmdCheckDuplicate = new SqlCommand(sqlCheckDuplicate, conn);
                    cmdCheckDuplicate.Parameters.AddWithValue("@subName", subName);
                    cmdCheckDuplicate.Parameters.AddWithValue("@containerName", containerName);

                    subCount = (int)cmdCheckDuplicate.ExecuteScalar();
                }
            }

            return subCount;
        }

        public int InsertNewSubscription(string resourceName, DateTime creationTimeDate, string containerName, string resType, int evt, string endpoint)
        {
            int rowsAffected = 0;
            string sqlInsert =
               @"
                INSERT INTO [subscription]
                ([resource-name], [creation-datetime], [container-resource-name], [res-type], [evt], [endpoint])
                VALUES (@resourceName, @creationDatetime, @containerResourceName, @resType, @evt, @endpoint)";


            using (var conn = new SqlConnection(connection))
            {
                using (conn)
                {
                    conn.Open();
                    var cmd = new SqlCommand(sqlInsert, conn);
                    cmd.Parameters.AddWithValue("@resourceName", resourceName);
                    cmd.Parameters.AddWithValue("@creationDatetime", creationTimeDate);
                    cmd.Parameters.AddWithValue(
                        "@containerResourceName",
                        containerName
                    );
                    cmd.Parameters.AddWithValue("@resType", resType);
                    cmd.Parameters.AddWithValue("@evt", evt);
                    cmd.Parameters.AddWithValue("@endpoint", endpoint);

                    rowsAffected = cmd.ExecuteNonQuery();
                }
            }
            return rowsAffected;
        }
    }
}
