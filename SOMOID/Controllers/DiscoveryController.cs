using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web.Http;
using Api.Routing;
using SOMOID.Models;

namespace SOMOID.Controllers
{
    public class DiscoveryController : ApiController
    {
        string connection = Properties.Settings.Default.ConnectionStr;

        // works as of now

        #region Discovery Actions

        [HttpGet]
        [GetRoute("api/somiod", discoveryResType: "application", false)]
        public IHttpActionResult DiscoverApplications()
        {
            var paths = new List<string>();
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    "SELECT [resource-name] FROM [application] ORDER BY [creation-datetime]",
                    conn
                )
            )
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

        [HttpGet]
        [GetRoute(
            "{appName:regex(^[^/]+$):applicationexists}",
            discoveryResType: "container",
            false
        )]
        public IHttpActionResult DiscoverContainers(string appName)
        {
            var containerPaths = new List<string>();
            using (var conn = new SqlConnection(connection))
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

        [HttpGet]
        [GetRoute(
            "{appName:regex(^[^/]+$):applicationexists}",
            discoveryResType: "content-instance",
            false
        )]
        public IHttpActionResult DiscoverContentInstances(string appName)
        {
            var paths = new List<string>();
            using (var conn = new SqlConnection(connection))
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

        [HttpGet]
        [GetRoute(
            "{appName:regex(^[^/]+$):applicationexists}/{containerName:regex(^[^/]+$):containerexists}",
            discoveryResType: "subscription",
            false
        )]
        public IHttpActionResult DiscoverSubscriptions(string appName, string containerName)
        {
            var subscriptionPaths = new List<string>();
            using (var conn = new SqlConnection(connection))
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

                try
                {
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
