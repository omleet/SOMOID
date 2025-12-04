using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Net.Http;
using System.Web.Http.Routing;
using SOMOID.Controllers; // Adjust if needed for access to connection string; alternatively, hardcode or refactor

namespace Api.Routing
{
    public class ApplicationExistsConstraint : IHttpRouteConstraint
    {
        public bool Match(HttpRequestMessage request, IHttpRoute route, string parameterName, IDictionary<string, object> values, HttpRouteDirection routeDirection)
        {
            if (routeDirection == HttpRouteDirection.UriGeneration)
                return true; // Allow URI generation without check

            if (values.TryGetValue(parameterName, out object value) && value is string appName)
            {
                string connection = SOMOID.Properties.Settings.Default.ConnectionStr;
                using (SqlConnection conn = new SqlConnection(connection))
                {
                    try
                    {
                        conn.Open();
                        string query = @"
                            SELECT COUNT(*)
                            FROM [application]
                            WHERE [resource-name] = @appName
                              AND [res-type] = @activeResType";
                        using (SqlCommand cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@appName", appName);
                            cmd.Parameters.AddWithValue("@activeResType", "application");
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