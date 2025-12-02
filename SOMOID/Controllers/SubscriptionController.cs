using SOMOID.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data.SqlClient;

namespace SOMOID.Controllers
{
    public class SubscriptionController : ApiController
    {
        string connection = Properties.Settings.Default.ConnectionStr;


        [HttpGet]
        [Route("")]
        public IHttpActionResult GetAllSubs()
        {
            var subs = new List<Subscription>();
            var conn = new SqlConnection(connection);

            string getQuery = @"
        SELECT [resource-name],
               [creation-datetime],  
               [container-resource-name],
               [res-type],
               [evt],
               [endpoint]
        FROM [subscription]";

            var cmd = new SqlCommand(getQuery, conn);

            try
            {
                using (conn)
                {
                    conn.Open();

                    var reader = cmd.ExecuteReader();

                    using (cmd)
                    {
                        using (reader)
                        {
                            while (reader.Read())
                            {
                                var sub = new Subscription
                                {
                                    ResourceName = (string)reader["resource-name"],
                                    CreationDatetime = (DateTime)reader["creation-datetime"],
                                    ContainerResourceName = (string)reader["container-resource-name"],
                                    ResType = (string)reader["res-type"],
                                    Evt = (int)reader["evt"],
                                    Endpoint = (string)reader["endpoint"]
                                };

                                subs.Add(sub);
                            }
                        }
                    }
                }
                return Ok(subs);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [Route("{appName}/{containerName}/{subName}")]
        public IHttpActionResult GetSubByName(string appName, string containerName, string subName)
        {
            Subscription sub = null;

            var conn = new SqlConnection(connection);

            string getQuery = @"
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
          AND s.[resource-name] = @subName";

            var cmd = new SqlCommand(getQuery, conn);

            cmd.Parameters.AddWithValue("@appName", appName);
            cmd.Parameters.AddWithValue("@containerName", containerName);
            cmd.Parameters.AddWithValue("@subName", subName);

            try
            {
                using (conn)
                {
                    conn.Open();

                    var reader = cmd.ExecuteReader();

                    using (cmd)
                    {
                        using (reader)
                        {
                            if (reader.Read())
                            {
                                sub = new Subscription
                                {
                                    ResourceName = (string)reader["resource-name"],
                                    CreationDatetime = (DateTime)reader["creation-datetime"],
                                    ContainerResourceName = (string)reader["container-resource-name"],
                                    ResType = (string)reader["res-type"],
                                    Evt = (int)reader["evt"],
                                    Endpoint = (string)reader["endpoint"]
                                };
                            }
                        }
                    }
                }

                if (sub == null)
                    return NotFound();
                return Ok(sub);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpPost]
        [Route("{appName}/{containerName}")]
        public IHttpActionResult Post(string appName, string containerName, [FromBody] Subscription value)
        {
            if (value == null)
                return BadRequest("There is no body at the moment.");


            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName = "sub-" + Guid.NewGuid().ToString();


            if (value.Evt != 1 && value.Evt != 2)
                return BadRequest("evt inválido (usa 1 ou 2).");

            if (string.IsNullOrWhiteSpace(value.Endpoint))
                return BadRequest("endpoint é obrigatório.");

            value.ResType = "subscription";
            value.ContainerResourceName = containerName;
            value.CreationDatetime = DateTime.UtcNow;

            string sqlCheckParent = @"SELECT COUNT(*)
        FROM [container] c JOIN [application] a
        ON a.[resource-name] = c.[application-resource-name]
        WHERE a.[resource-name] = @applicationName AND c.[resource-name] = @containerName";

            string sqlCheckDuplicate = @"SELECT COUNT(*)
        FROM [subscription]
        WHERE [resource-name] = @subName
        AND [container-resource-name] = @containerName";


            string sqlCommand = @"INSERT INTO [subscription]
            ([resource-name], [creation-datetime], [container-resource-name], [res-type], [evt], [endpoint])
            VALUES (@resourceName, @creationDatetime, @containerResourceName, @resType, @evt, @endpoint)";

            SqlConnection conn = new SqlConnection(connection);

            var cmd = new SqlCommand(sqlCommand, conn);
            var cmdCheckParent = new SqlCommand(sqlCheckParent, conn);
            var cmdCheckDuplicate = new SqlCommand(sqlCheckDuplicate, conn);

            try
            {
                using (conn)
                {

                    conn.Open();

                    // 1) Verificar se app + container existem
                    using (cmdCheckParent)
                    {
                        cmdCheckParent.Parameters.AddWithValue("@applicationName", appName);
                        cmdCheckParent.Parameters.AddWithValue("@containerName", containerName);

                        int containerCount = (int)cmdCheckParent.ExecuteScalar();

                        if (containerCount == 0)
                        {
                            return BadRequest("Application or Container does not exist.");
                        }
                    }

                    // 2) Verificar se já existe subscription com este nome
                    using (cmdCheckDuplicate)
                    {
                        cmdCheckDuplicate.Parameters.AddWithValue("@subName", value.ResourceName);
                        cmdCheckDuplicate.Parameters.AddWithValue("@containerName", containerName);

                        int subCount = (int)cmdCheckDuplicate.ExecuteScalar();

                        if (subCount > 0)
                        {
                            return BadRequest("Subscription with this name already exists.");
                        }
                    }

                    // 3) Inserir a nova subscription
                    using (cmd)
                    {
                        cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                        cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                        cmd.Parameters.AddWithValue("@containerResourceName", value.ContainerResourceName);
                        cmd.Parameters.AddWithValue("@resType", value.ResType);
                        cmd.Parameters.AddWithValue("@evt", value.Evt);
                        cmd.Parameters.AddWithValue("@endpoint", value.Endpoint);

                        int rowsAffected = cmd.ExecuteNonQuery();

                        if (rowsAffected > 0)
                        {
                            return Ok(value);
                        }
                        else
                        {
                            return InternalServerError(new Exception("Failed to create subscription."));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        [HttpDelete]
        [Route("{appName}/{containerName}/{subName}")]
        public IHttpActionResult DeleteSubscription(string appName, string containerName, string subName)
        {
            var conn = new SqlConnection(connection);

            string deleteQuery = @"
        DELETE s
        FROM [subscription] s
        JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
        WHERE a.[resource-name] = @appName
          AND c.[resource-name] = @containerName
          AND s.[resource-name] = @subName";

            var cmd = new SqlCommand(deleteQuery, conn);

            cmd.Parameters.AddWithValue("@appName", appName);
            cmd.Parameters.AddWithValue("@containerName", containerName);
            cmd.Parameters.AddWithValue("@subName", subName);

            try
            {
                using (conn)
                {
                    conn.Open();

                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        return NotFound();
                    }
                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}