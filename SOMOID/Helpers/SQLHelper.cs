using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

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
    }
}
