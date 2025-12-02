using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http;
using SOMOID.Models;

namespace SOMOID.Controllers
{
    [RoutePrefix("api/somiod")]
    public class DiscoveryController : ApiController
    {
        string connection = Properties.Settings.Default.ConnectionStr;

        [HttpGet]
        [Route("{appName?}/{containerName?}")]
        public IHttpActionResult Discover(string appName = null, string containerName = null)
        {
            IEnumerable<string> headers;
            if (!Request.Headers.TryGetValues("somiod-discovery", out headers))
                return BadRequest("Missing 'somiod-discovery' header");

            var resType = headers.FirstOrDefault()?.ToLower();
            if (resType == null)
                return BadRequest("Invalid 'somiod-discovery' header");

            switch (resType)
            {
                case "application":
                    return DiscoverApplications();

                case "container":
                    if (appName == null)
                        return BadRequest("appName is required for container discovery");
                    return DiscoverAllContainers(appName);

                case "content-instance":
                    if (appName == null)
                        return BadRequest("appName is required for content-instance discovery");
                    return DiscoverAllContentInstances(appName);

                case "subscription":
                    if (appName == null || containerName == null)
                        return BadRequest("appName and containerName are required for subscription discovery");
                    return DiscoverAllSubscriptions(appName, containerName);

                default:
                    return BadRequest($"Unknown discovery type '{resType}'");
            }
        }

        #region Private Discovery Methods

        private IHttpActionResult DiscoverApplications()
        {
            var paths = new List<string>();
            using (var conn = new SqlConnection(connection))
            using (var cmd = new SqlCommand("SELECT [resource-name] FROM [application] ORDER BY [creation-datetime]", conn))
            {
                try
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
                    return Ok(paths);
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
        }

        private IHttpActionResult DiscoverAllContainers(string appName)
        {
            var containerPaths = new List<string>();
            using (var conn = new SqlConnection(connection))
            using (var cmd = new SqlCommand(@"
                SELECT c.[resource-name]
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                ORDER BY c.[creation-datetime]", conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string contName = (string)reader["resource-name"];
                            containerPaths.Add($"/api/somiod/{appName}/{contName}");
                        }
                    }
                    return Ok(containerPaths);
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
        }

        private IHttpActionResult DiscoverAllContentInstances(string appName)
        {
            var paths = new List<string>();
            using (var conn = new SqlConnection(connection))
            using (var cmd = new SqlCommand(@"
                SELECT a.[resource-name] AS appName,
                       c.[resource-name] AS contName,
                       ci.[resource-name] AS ciName
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                ORDER BY a.[resource-name], c.[resource-name], ci.[creation-datetime]", conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);

                try
                {
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
                    return Ok(paths);
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
        }

        private IHttpActionResult DiscoverAllSubscriptions(string appName, string containerName)
        {
            var subscriptionPaths = new List<string>();
            using (var conn = new SqlConnection(connection))
            using (var cmd = new SqlCommand(@"
                SELECT s.[resource-name]
                FROM [subscription] s
                JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                ORDER BY s.[creation-datetime]", conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);

                try
                {
                    conn.Open();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string subName = (string)reader["resource-name"];
                            subscriptionPaths.Add($"/api/somiod/{appName}/{containerName}/subs/{subName}");
                        }
                    }
                    return Ok(subscriptionPaths);
                }
                catch (Exception ex)
                {
                    return InternalServerError(ex);
                }
            }
        }

        #endregion
    }
}
