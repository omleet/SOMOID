using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Web.Http.Routing;

namespace Api.Routing
{
    public class ContainerExistsConstraint : IHttpRouteConstraint
    {
        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            if (routeDirection == HttpRouteDirection.UriGeneration)
                return true;

            if (values.TryGetValue("appName", out object appObj) && appObj is string appName &&
                values.TryGetValue("containerName", out object contObj) && contObj is string containerName)
            {
                string connection = SOMOID.Properties.Settings.Default.ConnectionStr;
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    try
                    {
                        conn.Open();
                        string query = @"
                            SELECT COUNT(*)
                            FROM [container]
                            WHERE [resource-name] = @containerName
                              AND [application-resource-name] = @appName";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@containerName", containerName);
                            cmd.Parameters.AddWithValue("@appName", appName);
                            int count = (int)cmd.ExecuteScalar();
                            return count > 0;
                        }
                    }
                    catch
                    {
                        return false; // Fail closed on error
                    }
                }
            }
            return false;
        }
    }
}