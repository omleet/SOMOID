using System;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using SOMOID.Models;

namespace SOMOID.Helpers
{
    public class SQLHelper
    {
        private const string ActiveApplicationResType = "application";
        private const string DeletedApplicationResType = "application-deleted";
        private readonly string connection = SOMOID.Properties.Settings.Default.ConnectionStr;

        static SQLHelper()
        {
            EnsureScopedSchema();
        }

        public List<string> GetAllApplications()
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

                        ORDER BY a.[resource-name], c.[creation-datetime]",
                    conn
                )
            )
            {
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
            return containerPaths;
        }

        public List<string> GetAllContentInstances()
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
                                    AND c.[application-resource-name] = ci.[application-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                
                ORDER BY a.[resource-name], c.[resource-name], ci.[creation-datetime]",
                    conn
                )
            )
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
            }
            return paths;
        }

        public List<string> GetAllSubscriptions()
        {
            var subscriptionPaths = new List<string>();
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT a.[resource-name] AS appName,
                       c.[resource-name] AS contName,
                       s.[resource-name] AS subName
                FROM [subscription] s
                JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                                    AND c.[application-resource-name] = s.[application-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                
                ORDER BY a.[resource-name], c.[resource-name], s.[creation-datetime]",
                    conn
                )
            )
            {
                
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string appName = (string)reader["appName"];
                        string contName = (string)reader["contName"];
                        string subName = (string)reader["subName"];
                        subscriptionPaths.Add($"/api/somiod/{appName}/{contName}/subs/{subName}");
                    }
                }
            }
            return subscriptionPaths;
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
                                         AND c.[application-resource-name] = s.[application-resource-name]
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
            return subscriptionPaths;
        }

        public Subscription GetSubscriptionByAppName(
            string appName,
            string containerName,
            string subName
        )
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
            SELECT s.[resource-name],
                   s.[creation-datetime],
                   s.[container-resource-name],
                   s.[application-resource-name],
                  
                   s.[evt],
                   s.[endpoint]
            FROM [subscription] s
            JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                                 AND c.[application-resource-name] = s.[application-resource-name]
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
              AND c.[resource-name] = @containerName
              AND s.[resource-name] = @subName
              ",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@subName", subName);
                
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
                            ApplicationResourceName = (string)reader["application-resource-name"],
                            
                            Evt = (int)reader["evt"],
                            Endpoint = (string)reader["endpoint"],
                        };
                    }
                }
            }
            return null;
        }

        public List<Subscription> GetSubscriptionsForContainer(string appName, string containerName)
        {
            var subscriptions = new List<Subscription>();
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
            SELECT s.[resource-name],
                   s.[creation-datetime],
                   s.[container-resource-name],
                   s.[application-resource-name],
                   
                   s.[evt],
                   s.[endpoint]
            FROM [subscription] s
            JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                                 AND c.[application-resource-name] = s.[application-resource-name]
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
              AND c.[resource-name] = @containerName
              ",
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
                        subscriptions.Add(
                            new Subscription
                            {
                                ResourceName = (string)reader["resource-name"],
                                CreationDatetime = (DateTime)reader["creation-datetime"],
                                ContainerResourceName = (string)reader["container-resource-name"],
                                ApplicationResourceName = (string)reader["application-resource-name"],
                               
                                Evt = (int)reader["evt"],
                                Endpoint = (string)reader["endpoint"],
                            }
                        );
                    }
                }
            }
            return subscriptions;
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
                ";
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                var cmdCheckParent = new SqlCommand(sqlCheckParent, conn);
                cmdCheckParent.Parameters.AddWithValue("@applicationName", appName);
                cmdCheckParent.Parameters.AddWithValue("@containerName", containerName);
                containerCount = (int)cmdCheckParent.ExecuteScalar();
            }
            return containerCount;
        }

        public int CheckIfSubscriptionAlreadyExists(string appName, string containerName, string subName)
        {
            int subCount = 0;
            string sqlCheckDuplicate =
                @"
                SELECT COUNT(*)
                FROM [subscription]
                WHERE [resource-name] = @subName
                AND [container-resource-name] = @containerName
                AND [application-resource-name] = @appName";
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                var cmdCheckDuplicate = new SqlCommand(sqlCheckDuplicate, conn);
                cmdCheckDuplicate.Parameters.AddWithValue("@subName", subName);
                cmdCheckDuplicate.Parameters.AddWithValue("@containerName", containerName);
                cmdCheckDuplicate.Parameters.AddWithValue("@appName", appName);
                subCount = (int)cmdCheckDuplicate.ExecuteScalar();
            }
            return subCount;
        }

        public int InsertNewSubscription(
            string resourceName,
            DateTime creationTimeDate,
            string containerName,
            string appName,
            string resType,
            int evt,
            string endpoint
        )
        {
            int rowsAffected = 0;
            string sqlInsert =
                @"
                INSERT INTO [subscription]
                ([resource-name], [creation-datetime], [container-resource-name], [application-resource-name], [evt], [endpoint])
                VALUES (@resourceName, @creationDatetime, @containerResourceName, @appName, @resType, @evt, @endpoint)";
            using (var conn = new SqlConnection(connection))
            {
                conn.Open();
                var cmd = new SqlCommand(sqlInsert, conn);
                cmd.Parameters.AddWithValue("@resourceName", resourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", creationTimeDate);
                cmd.Parameters.AddWithValue("@containerResourceName", containerName);
                cmd.Parameters.AddWithValue("@appName", appName);
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
                       [creation-datetime]
                      
                FROM [application]
                WHERE [resource-name] = @appName
                  ",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Application
                        {
                            ResourceName = (string)reader["resource-name"],
                            CreationDatetime = (DateTime)reader["creation-datetime"],
 
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
                SELECT *
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
                ([resource-name], [creation-datetime])
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
                      ",
                        conn
                    )
                )
                {
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@currentName", currentName);
                    var rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return null;
                }

                using (
                    var cmdGet = new SqlCommand(
                        @"
                    SELECT [resource-name],
                           [creation-datetime],

                    FROM [application]
                    WHERE [resource-name] = @appName
                      ",
                        conn
                    )
                )
                {
                    cmdGet.Parameters.AddWithValue("@appName", newName);
                    using (var reader = cmdGet.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Application
                            {
                                ResourceName = (string)reader["resource-name"],
                                CreationDatetime = (DateTime)reader["creation-datetime"],
                            };
                        }
                    }
                }
            }
            return null;
        }

        
        public bool HardDeleteApplication(string appName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                DELETE [application]
                WHERE [resource-name] = @appName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
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
                WHERE [resource-name] = @appName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
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

                       c.[application-resource-name]
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Container
                        {
                            ResourceName = (string)reader["resource-name"],
                            CreationDatetime = (DateTime)reader["creation-datetime"],
                
                            ApplicationResourceName = (string)reader["application-resource-name"],
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
                  ",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
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
                ([resource-name], [creation-datetime], [application-resource-name])
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
                    SELECT [resource-name], [creation-datetime], [application-resource-name]
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
                                ApplicationResourceName = (string)
                                    reader["application-resource-name"],
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
                using (
                    var cmdDelCI = new SqlCommand(
                        @"
                        DELETE ci
                        FROM [content-instance] ci
                        JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                                             AND c.[application-resource-name] = ci.[application-resource-name]
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName
                          AND ci.[application-resource-name] = @appName",
                        conn
                    )
                )
                {
                    cmdDelCI.Parameters.AddWithValue("@appName", appName);
                    cmdDelCI.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelCI.ExecuteNonQuery();
                }

                using (
                    var cmdDelSub = new SqlCommand(
                        @"
                        DELETE s
                        FROM [subscription] s
                        JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                                             AND c.[application-resource-name] = s.[application-resource-name]
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName
                          AND s.[application-resource-name] = @appName",
                        conn
                    )
                )
                {
                    cmdDelSub.Parameters.AddWithValue("@appName", appName);
                    cmdDelSub.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelSub.ExecuteNonQuery();
                }

                using (
                    var cmdDelContainer = new SqlCommand(
                        @"
                        DELETE c
                        FROM [container] c
                        JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                        WHERE a.[resource-name] = @appName
                          AND c.[resource-name] = @containerName",
                        conn
                    )
                )
                {
                    cmdDelContainer.Parameters.AddWithValue("@appName", appName);
                    cmdDelContainer.Parameters.AddWithValue("@containerName", containerName);
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
                  ",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
               
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public bool ContentInstanceExistsInContainer(string appName, string containerName, string ciName)
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT COUNT(*)
                FROM [content-instance]
                WHERE [resource-name] = @ciName
                  AND [container-resource-name] = @containerName
                  AND [application-resource-name] = @appName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@ciName", ciName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@appName", appName);
                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        public ContentInstance GetContentInstance(
            string appName,
            string containerName,
            string ciName
        )
        {
            using (var conn = new SqlConnection(connection))
            using (
                var cmd = new SqlCommand(
                    @"
                SELECT ci.[resource-name],
                       ci.[creation-datetime],
                       ci.[container-resource-name],
                       ci.[application-resource-name],
                       ci.[res-type],
                       ci.[content-type],
                       ci.[content]
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                                    AND c.[application-resource-name] = ci.[application-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND ci.[resource-name] = @ciName
                  ",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@ciName", ciName);
                
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
                            ApplicationResourceName = (string)reader["application-resource-name"],
                            ResType = (string)reader["res-type"],
                            ContentType = (string)reader["content-type"],
                            Content = (string)reader["content"],
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
                    ([resource-name], [creation-datetime], [container-resource-name], [application-resource-name],
                     [res-type], [content-type], [content])
                VALUES (@resourceName, @creationDatetime, @containerResourceName, @appResourceName,
                        @resType, @contentType, @content)",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                cmd.Parameters.AddWithValue("@containerResourceName", value.ContainerResourceName);
                cmd.Parameters.AddWithValue("@appResourceName", value.ApplicationResourceName);
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
                                     AND c.[application-resource-name] = ci.[application-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND ci.[resource-name] = @ciName
                  AND ci.[application-resource-name] = @appName
                  ",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@ciName", ciName);
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
                                     AND c.[application-resource-name] = s.[application-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                AND c.[resource-name] = @containerName
                AND s.[resource-name] = @subName
                ",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@subName", subName);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public List<string> GetAllContentInstancesFromApp(string appName)
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
                                    AND c.[application-resource-name] = ci.[application-resource-name]
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
            return paths;
        }

        private static void EnsureScopedSchema()
        {
            try
            {
                using (var conn = new SqlConnection(SOMOID.Properties.Settings.Default.ConnectionStr))
                {
                    conn.Open();
                    DropForeignKeysReferencingContainer(conn, "content-instance");
                    DropForeignKeysReferencingContainer(conn, "subscription");
                    EnsureColumnExists(conn, "content-instance", "application-resource-name", "NVARCHAR(50) NULL");
                    EnsureColumnExists(conn, "subscription", "application-resource-name", "NVARCHAR(50) NULL");
                    BackfillContentInstanceApplications(conn);
                    BackfillSubscriptionApplications(conn);
                    EnsureColumnIsNotNull(conn, "content-instance", "application-resource-name", "NVARCHAR(50) NOT NULL");
                    EnsureColumnIsNotNull(conn, "subscription", "application-resource-name", "NVARCHAR(50) NOT NULL");
                    EnsurePrimaryKey(
                        conn,
                        "container",
                        "PK_container_scoped",
                        "[resource-name], [application-resource-name]"
                    );
                    EnsurePrimaryKey(
                        conn,
                        "content-instance",
                        "PK_content_instance_scoped",
                        "[resource-name], [container-resource-name], [application-resource-name]"
                    );
                    EnsurePrimaryKey(
                        conn,
                        "subscription",
                        "PK_subscription_scoped",
                        "[resource-name], [container-resource-name], [application-resource-name]"
                    );
                    EnsureForeignKey(
                        conn,
                        "content-instance",
                        "FK_content_instance_container_scoped",
                        "[container-resource-name], [application-resource-name]",
                        "container",
                        "[resource-name], [application-resource-name]"
                    );
                    EnsureForeignKey(
                        conn,
                        "subscription",
                        "FK_subscription_container_scoped",
                        "[container-resource-name], [application-resource-name]",
                        "container",
                        "[resource-name], [application-resource-name]"
                    );
                }
            }
            catch
            {
            }
        }

        private static void EnsurePrimaryKey(
            SqlConnection conn,
            string tableName,
            string desiredConstraintName,
            string columnList
        )
        {
            string currentPkName = null;
            var currentColumns = new List<string>();
            using (
                var cmd = new SqlCommand(
                    @"
            SELECT kc.name
            FROM sys.key_constraints kc
            JOIN sys.tables t ON kc.parent_object_id = t.object_id
            WHERE kc.type = 'PK'
              AND t.name = @tableName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@tableName", tableName);
                var result = cmd.ExecuteScalar();
                currentPkName = result as string;
            }

            if (!string.IsNullOrEmpty(currentPkName))
            {
                using (
                    var columnCmd = new SqlCommand(
                        @"
            SELECT c.name
            FROM sys.key_constraints kc
            JOIN sys.tables t ON kc.parent_object_id = t.object_id
            JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE kc.type = 'PK'
              AND t.name = @tableName
              AND kc.name = @pkName
            ORDER BY ic.key_ordinal",
                        conn
                    )
                )
                {
                    columnCmd.Parameters.AddWithValue("@tableName", tableName);
                    columnCmd.Parameters.AddWithValue("@pkName", currentPkName);
                    using (var reader = columnCmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            currentColumns.Add((string)reader["name"]);
                        }
                    }
                }
            }

            var desiredColumns = columnList
                .Split(',')
                .Select(col => col.Replace("[", string.Empty).Replace("]", string.Empty).Trim())
                .Where(col => !string.IsNullOrEmpty(col))
                .ToList();

            bool nameMatches = string.Equals(
                currentPkName,
                desiredConstraintName,
                StringComparison.OrdinalIgnoreCase
            );

            bool columnsMatch =
                currentColumns.Count == desiredColumns.Count
                && currentColumns.SequenceEqual(desiredColumns, StringComparer.OrdinalIgnoreCase);

            if (nameMatches && columnsMatch)
            {
                return;
            }

            if (!string.IsNullOrEmpty(currentPkName))
            {
                using (
                    var dropCmd = new SqlCommand(
                        $"ALTER TABLE [{tableName}] DROP CONSTRAINT [{currentPkName}]",
                        conn
                    )
                )
                {
                    dropCmd.ExecuteNonQuery();
                }
            }

            using (
                var addCmd = new SqlCommand(
                    $"ALTER TABLE [{tableName}] ADD CONSTRAINT [{desiredConstraintName}] PRIMARY KEY ({columnList})",
                    conn
                )
            )
            {
                addCmd.ExecuteNonQuery();
            }
        }

        private static void EnsureColumnExists(
            SqlConnection conn,
            string tableName,
            string columnName,
            string columnDefinition
        )
        {
            using (
                var cmd = new SqlCommand(
                    @"
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@tableName", tableName);
                cmd.Parameters.AddWithValue("@columnName", columnName);
                var exists = (int)cmd.ExecuteScalar() > 0;
                if (exists)
                    return;
            }

            using (
                var addCmd = new SqlCommand(
                    $"ALTER TABLE [{tableName}] ADD [{columnName}] {columnDefinition}",
                    conn
                )
            )
            {
                addCmd.ExecuteNonQuery();
            }
        }

        private static void EnsureColumnIsNotNull(
            SqlConnection conn,
            string tableName,
            string columnName,
            string columnDefinition
        )
        {
            string currentNullability = null;
            using (
                var cmd = new SqlCommand(
                    @"
            SELECT IS_NULLABLE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@tableName", tableName);
                cmd.Parameters.AddWithValue("@columnName", columnName);
                var result = cmd.ExecuteScalar();
                currentNullability = result as string;
            }

            if (!string.Equals(currentNullability, "YES", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            using (
                var alterCmd = new SqlCommand(
                    $"ALTER TABLE [{tableName}] ALTER COLUMN [{columnName}] {columnDefinition}",
                    conn
                )
            )
            {
                alterCmd.ExecuteNonQuery();
            }
        }

        private static void BackfillContentInstanceApplications(SqlConnection conn)
        {
            using (
                var cmd = new SqlCommand(
                    @"
            UPDATE ci
            SET [application-resource-name] = c.[application-resource-name]
            FROM [content-instance] ci
            JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
            WHERE ci.[application-resource-name] IS NULL",
                    conn
                )
            )
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void BackfillSubscriptionApplications(SqlConnection conn)
        {
            using (
                var cmd = new SqlCommand(
                    @"
            UPDATE s
            SET [application-resource-name] = c.[application-resource-name]
            FROM [subscription] s
            JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
            WHERE s.[application-resource-name] IS NULL",
                    conn
                )
            )
            {
                cmd.ExecuteNonQuery();
            }
        }

        private static void DropForeignKeysReferencingContainer(SqlConnection conn, string tableName)
        {
            var fkNames = new List<string>();
            string query = $@"
            SELECT fk.name
            FROM sys.foreign_keys fk
            WHERE fk.parent_object_id = OBJECT_ID(N'[dbo].[{tableName}]')
              AND fk.referenced_object_id = OBJECT_ID(N'[dbo].[container]')";
            using (var cmd = new SqlCommand(query, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    fkNames.Add((string)reader["name"]);
                }
            }

            foreach (var fkName in fkNames)
            {
                using (var dropCmd = new SqlCommand(
                           $"ALTER TABLE [{tableName}] DROP CONSTRAINT [{fkName}]",
                           conn
                       ))
                {
                    dropCmd.ExecuteNonQuery();
                }
            }
        }

        private static void EnsureForeignKey(
            SqlConnection conn,
            string tableName,
            string constraintName,
            string columnList,
            string referencedTable,
            string referencedColumnList
        )
        {
            using (
                var cmd = new SqlCommand(
                    @"SELECT COUNT(*) FROM sys.foreign_keys WHERE name = @name",
                    conn
                )
            )
            {
                cmd.Parameters.AddWithValue("@name", constraintName);
                if ((int)cmd.ExecuteScalar() > 0)
                    return;
            }

            using (
                var addCmd = new SqlCommand(
                    $"ALTER TABLE [{tableName}] ADD CONSTRAINT [{constraintName}] FOREIGN KEY ({columnList}) REFERENCES [{referencedTable}] ({referencedColumnList})",
                    conn
                )
            )
            {
                addCmd.ExecuteNonQuery();
            }
        }
    }
}
