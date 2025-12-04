using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using SOMOID.Models;

namespace SOMOID.Helpers
{
    public class SQLHelper
    {
        private const string ActiveApplicationResType = "application";
        private const string DeletedApplicationResType = "application-deleted";
        private readonly string connection = SOMOID.Properties.Settings.Default.ConnectionStr;

        public List<string> GetAllApplications()
        {
            var paths = new List<string>();
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    "SELECT [resource-name] FROM [application] WHERE [res-type] = @active ORDER BY [creation-datetime]",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
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
            return paths;
        }

        public List<string> GetAllContainers()
        {
            var containerPaths = new List<string>();
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                        SELECT a.[resource-name] AS appName,
                               c.[resource-name] AS contName
                        FROM [container] c
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[res-type] = @active
                        ORDER BY a.[resource-name], c.[creation-datetime]",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string appName = (string)reader["appName"];
                        string contName = (string)reader["contName"];
                        containerPaths.Add($"/api/somiod/{appName}/{contName}");
                    }
                }
            }
            return containerPaths;
        }

        public List<string> GetAllContainers(string appName)
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
                          AND a.[res-type] = @active
                        ORDER BY c.[creation-datetime]",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
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
            return containerPaths;
        }

        public List<string> GetAllContentInstances(string appName)
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
                          AND a.[res-type] = @active
                        ORDER BY a.[resource-name], c.[resource-name], ci.[creation-datetime]",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
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
            return paths;
        }

        public List<string> GetAllSubscriptions(string appName, string containerName)
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
                    AND a.[res-type] = @active
                    ORDER BY s.[creation-datetime]",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string subName = (string)reader["resource-name"];
                        subscriptionPaths.Add($"/api/somiod/{appName}/{containerName}/subs/{subName}");
                    }
                }
            }
            return subscriptionPaths;
        }

        public Subscription GetSubscriptionByAppName(string appName, string containerName, string subName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
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
              AND s.[resource-name] = @subName
              AND a.[res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@subName", subName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Subscription
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
                AND c.[resource-name] = @containerName
                AND a.[res-type] = @active";
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                var cmdCheckParent = new SqlCommand(sqlCheckParent, conn);
                cmdCheckParent.Parameters.AddWithValue("@applicationName", appName);
                cmdCheckParent.Parameters.AddWithValue("@containerName", containerName);
                cmdCheckParent.Parameters.AddWithValue("@active", ActiveApplicationResType);
                containerCount = (int)cmdCheckParent.ExecuteScalar();
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
                conn.Open();
                var cmdCheckDuplicate = new SqlCommand(sqlCheckDuplicate, conn);
                cmdCheckDuplicate.Parameters.AddWithValue("@subName", subName);
                cmdCheckDuplicate.Parameters.AddWithValue("@containerName", containerName);
                subCount = (int)cmdCheckDuplicate.ExecuteScalar();
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
                conn.Open();
                var cmd = new SqlCommand(sqlInsert, conn);
                cmd.Parameters.AddWithValue("@resourceName", resourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", creationTimeDate);
                cmd.Parameters.AddWithValue("@containerResourceName", containerName);
                cmd.Parameters.AddWithValue("@resType", resType);
                cmd.Parameters.AddWithValue("@evt", evt);
                cmd.Parameters.AddWithValue("@endpoint", endpoint);
                rowsAffected = cmd.ExecuteNonQuery();
            }
            return rowsAffected;
        }

        public Application GetApplication(string appName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT [resource-name],
                       [creation-datetime],
                       [res-type]
                FROM [application]
                WHERE [resource-name] = @appName
                  AND [res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Application
                        {
                            ResourceName = (string)reader["resource-name"],
                            CreationDatetime = (DateTime)reader["creation-datetime"],
                            ResType = (string)reader["res-type"]
                        };
                    }
                }
            }
            return null;
        }

        public string GetApplicationResTypeValue(string appName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT TOP 1 [res-type]
                FROM [application]
                WHERE [resource-name] = @appName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                conn.Open();
                var result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? null : (string)result;
            }
        }

        public bool InsertApplication(Application value)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                INSERT INTO [application]
                ([resource-name], [creation-datetime], [res-type])
                VALUES (@resourceName, @creationDatetime, @resType)",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                cmd.Parameters.AddWithValue("@resType", value.ResType);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public Application RenameApplication(string currentName, string newName)
        {
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                using (
                    var cmd = new SqlCommand(
                        @"
                    UPDATE [application]
                    SET [resource-name] = @newName
                    WHERE [resource-name] = @currentName
                      AND [res-type] = @active",
                        conn
                    )
                )
                {
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@currentName", currentName);
                    cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                    var rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return null;
                }

                using (
                    var cmdGet = new SqlCommand(
                        @"
                    SELECT [resource-name],
                           [creation-datetime],
                           [res-type]
                    FROM [application]
                    WHERE [resource-name] = @appName
                      AND [res-type] = @active",
                        conn
                    )
                )
                {
                    cmdGet.Parameters.AddWithValue("@appName", newName);
                    cmdGet.Parameters.AddWithValue("@active", ActiveApplicationResType);
                    using (var reader = cmdGet.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Application
                            {
                                ResourceName = (string)reader["resource-name"],
                                CreationDatetime = (DateTime)reader["creation-datetime"],
                                ResType = (string)reader["res-type"]
                            };
                        }
                    }
                }
            }
            return null;
        }

        public bool SoftDeleteApplication(string appName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                UPDATE [application]
                SET [res-type] = @deleted
                WHERE [resource-name] = @appName
                  AND [res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@deleted", DeletedApplicationResType);
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool ApplicationExists(string appName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT COUNT(*)
                FROM [application]
                WHERE [resource-name] = @appName
                  AND [res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public Container GetContainer(string appName, string containerName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT c.[resource-name],
                       c.[creation-datetime],
                       c.[res-type],
                       c.[application-resource-name]
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND a.[res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Container
                        {
                            ResourceName = (string)reader["resource-name"],
                            CreationDatetime = (DateTime)reader["creation-datetime"],
                            ResType = (string)reader["res-type"],
                            ApplicationResourceName = (string)reader["application-resource-name"]
                        };
                    }
                }
            }
            return null;
        }

        public bool ContainerExists(string appName, string containerName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT COUNT(*)
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND a.[res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public bool ContainerNameExists(string appName, string containerName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT COUNT(*)
                FROM [container]
                WHERE [resource-name] = @containerName
                  AND [application-resource-name] = @appName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@appName", appName);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public bool InsertContainer(Container value)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                INSERT INTO [container]
                ([resource-name], [creation-datetime], [res-type], [application-resource-name])
                VALUES (@resourceName, @creationDatetime, @resType, @appResourceName)",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                cmd.Parameters.AddWithValue("@resType", value.ResType);
                cmd.Parameters.AddWithValue("@appResourceName", value.ApplicationResourceName);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public Container RenameContainer(string appName, string containerName, string newName)
        {
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                using (
                    var cmd = new SqlCommand(
                        @"
                    UPDATE [container]
                    SET [resource-name] = @newName
                    WHERE [resource-name] = @oldName
                      AND [application-resource-name] = @appName",
                        conn
                    )
                )
                {
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@oldName", containerName);
                    cmd.Parameters.AddWithValue("@appName", appName);
                    var rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return null;
                }

                using (
                    var cmdGet = new SqlCommand(
                        @"
                    SELECT [resource-name], [creation-datetime], [res-type], [application-resource-name]
                    FROM [container]
                    WHERE [resource-name] = @name
                      AND [application-resource-name] = @appName",
                        conn
                    )
                )
                {
                    cmdGet.Parameters.AddWithValue("@name", newName);
                    cmdGet.Parameters.AddWithValue("@appName", appName);
                    using (var reader = cmdGet.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Container
                            {
                                ResourceName = (string)reader["resource-name"],
                                CreationDatetime = (DateTime)reader["creation-datetime"],
                                ResType = (string)reader["res-type"],
                                ApplicationResourceName = (string)reader["application-resource-name"]
                            };
                        }
                    }
                }
            }
            return null;
        }

        public bool DeleteContainerCascade(string appName, string containerName)
        {
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                using (var cmdDelCI = new SqlCommand(
                    @"
                        DELETE ci
                        FROM [content-instance] ci
                        JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName
                          AND a.[res-type] = @active",
                    conn))
                {
                    cmdDelCI.Parameters.AddWithValue("@appName", appName);
                    cmdDelCI.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelCI.Parameters.AddWithValue("@active", ActiveApplicationResType);
                    cmdDelCI.ExecuteNonQuery();
                }

                using (var cmdDelSub = new SqlCommand(
                    @"
                        DELETE s
                        FROM [subscription] s
                        JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName
                          AND a.[res-type] = @active",
                    conn))
                {
                    cmdDelSub.Parameters.AddWithValue("@appName", appName);
                    cmdDelSub.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelSub.Parameters.AddWithValue("@active", ActiveApplicationResType);
                    cmdDelSub.ExecuteNonQuery();
                }

                using (var cmdDelContainer = new SqlCommand(
                    @"
                        DELETE c
                        FROM [container] c
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName
                          AND a.[res-type] = @active",
                    conn))
                {
                    cmdDelContainer.Parameters.AddWithValue("@appName", appName);
                    cmdDelContainer.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelContainer.Parameters.AddWithValue("@active", ActiveApplicationResType);
                    return cmdDelContainer.ExecuteNonQuery() > 0;
                }
            }
        }

        public bool ContentInstanceParentExists(string appName, string containerName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT COUNT(*)
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND a.[res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public bool ContentInstanceExistsInContainer(string containerName, string ciName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT COUNT(*)
                FROM [content-instance]
                WHERE [resource-name] = @ciName
                  AND [container-resource-name] = @containerName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@ciName", ciName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public ContentInstance GetContentInstance(string appName, string containerName, string ciName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT ci.[resource-name],
                       ci.[creation-datetime],
                       ci.[container-resource-name],
                       ci.[res-type],
                       ci.[content-type],
                       ci.[content]
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND ci.[resource-name] = @ciName
                  AND a.[res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@ciName", ciName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ContentInstance
                        {
                            ResourceName = (string)reader["resource-name"],
                            CreationDatetime = (DateTime)reader["creation-datetime"],
                            ContainerResourceName = (string)reader["container-resource-name"],
                            ResType = (string)reader["res-type"],
                            ContentType = (string)reader["content-type"],
                            Content = (string)reader["content"]
                        };
                    }
                }
            }
            return null;
        }

        public bool InsertContentInstance(ContentInstance value)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                INSERT INTO [content-instance]
                    ([resource-name], [creation-datetime], [container-resource-name],
                     [res-type], [content-type], [content])
                VALUES (@resourceName, @creationDatetime, @containerResourceName,
                        @resType, @contentType, @content)",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                cmd.Parameters.AddWithValue("@containerResourceName", value.ContainerResourceName);
                cmd.Parameters.AddWithValue("@resType", value.ResType);
                cmd.Parameters.AddWithValue("@contentType", value.ContentType);
                cmd.Parameters.AddWithValue("@content", value.Content);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteContentInstance(string appName, string containerName, string ciName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                DELETE ci
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND ci.[resource-name] = @ciName
                  AND a.[res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@ciName", ciName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool DeleteSubscription(string appName, string containerName, string subName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                DELETE s
                FROM [subscription] s
                JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                AND c.[resource-name] = @containerName
                AND s.[resource-name] = @subName
                AND a.[res-type] = @active",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@subName", subName);
                cmd.Parameters.AddWithValue("@active", ActiveApplicationResType);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
