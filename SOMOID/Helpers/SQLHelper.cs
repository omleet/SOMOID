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

        /// <summary>
        /// Retrieves all application names as API paths.
        /// </summary>
        /// <returns>List of application API paths in the format /api/somiod/{appName}</returns>
        public List<string> GetAllApplications()
        {
            List<string> paths = new List<string>();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
                "SELECT [resource-name] FROM [application] ORDER BY [creation-datetime]",
                conn
            ))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string appName = (string)reader["resource-name"];
                        paths.Add(string.Format("/api/somiod/{0}", appName));
                    }
                }
            }

            return paths;
        }

        /// <summary>
        /// Retrieves all container names across all applications as API paths.
        /// </summary>
        /// <returns>List of container API paths in the format /api/somiod/{appName}/{containerName}</returns>
        public List<string> GetAllContainers()
        {
            List<string> containerPaths = new List<string>();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
                @"
            SELECT a.[resource-name] AS appName,
                   c.[resource-name] AS contName
            FROM [container] c
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            ORDER BY a.[resource-name], c.[creation-datetime]",
                conn
            ))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string appName = (string)reader["appName"];
                        string contName = (string)reader["contName"];
                        containerPaths.Add(string.Format("/api/somiod/{0}/{1}", appName, contName));
                    }
                }
            }

            return containerPaths;
        }

        /// <summary>
        /// Retrieves all container names for a specific application as API paths.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <returns>List of container API paths in the format /api/somiod/{appName}/{containerName}</returns>
        public List<string> GetAllContainers(string appName)
        {
            List<string> containerPaths = new List<string>();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
                @"
            SELECT c.[resource-name] AS containerName
            FROM [container] c
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
            ORDER BY c.[creation-datetime]",
                conn
            ))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string containerName = (string)reader["containerName"];
                        // Build API path using string interpolation
                        string path = $"/api/somiod/{appName}/{containerName}";
                        containerPaths.Add(path);
                    }
                }
            }

            return containerPaths;
        }

        /// <summary>
        /// Retrieves all content instances across all applications and containers as API paths.
        /// </summary>
        /// <returns>List of content instance API paths in the format /api/somiod/{appName}/{containerName}/{contentInstanceName}</returns>
        public List<string> GetAllContentInstances()
        {
            List<string> paths = new List<string>();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
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
            ))
            {
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string appName = (string)reader["appName"];
                        string containerName = (string)reader["contName"];
                        string ciName = (string)reader["ciName"];
                        // Build API path using string interpolation
                        string path = $"/api/somiod/{appName}/{containerName}/{ciName}";
                        paths.Add(path);
                    }
                }
            }

            return paths;
        }


        /// <summary>
        /// Retrieves all subscriptions across all applications and containers as API paths.
        /// </summary>
        /// <returns>List of subscription API paths in the format /api/somiod/{appName}/{containerName}/subs/{subName}</returns>
        public List<string> GetAllSubscriptions()
        {
            List<string> subscriptionPaths = new List<string>();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
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
            ))
            {
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string appName = (string)reader["appName"];
                        string containerName = (string)reader["contName"];
                        string subName = (string)reader["subName"];
                        // Build API path using string interpolation
                        string path = $"/api/somiod/{appName}/{containerName}/subs/{subName}";
                        subscriptionPaths.Add(path);
                    }
                }
            }

            return subscriptionPaths;
        }

        /// <summary>
        /// Retrieves all subscriptions for a specific application and container as API paths.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <returns>List of subscription API paths in the format /api/somiod/{appName}/{containerName}/subs/{subName}</returns>
        public List<string> GetAllSubscriptions(string appName, string containerName)
        {
            List<string> subscriptionPaths = new List<string>();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
                @"
            SELECT s.[resource-name] AS subName
            FROM [subscription] s
            JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                                 AND c.[application-resource-name] = s.[application-resource-name]
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
            AND c.[resource-name] = @containerName
            ORDER BY s.[creation-datetime]",
                conn
            ))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string subName = (string)reader["subName"];
                        // Build API path using string interpolation
                        string path = $"/api/somiod/{appName}/{containerName}/subs/{subName}";
                        subscriptionPaths.Add(path);
                    }
                }
            }

            return subscriptionPaths;
        }


        /// <summary>
        /// Retrieves a subscription by application name, container name, and subscription name.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="subName">The name of the subscription.</param>
        /// <returns>A Subscription object if found; otherwise, null.</returns>
        public Subscription GetSubscriptionByAppName(
            string appName,
            string containerName,
            string subName
        )
        {
            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
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
              AND s.[resource-name] = @subName",
                conn
            ))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@subName", subName);

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
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
                            Endpoint = (string)reader["endpoint"]
                        };
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves all subscriptions for a specific application and container.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <returns>List of Subscription objects for the specified container.</returns>
        public List<Subscription> GetSubscriptionsForContainer(string appName, string containerName)
        {
            List<Subscription> subscriptions = new List<Subscription>();

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
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
              AND c.[resource-name] = @containerName",
                conn
            ))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
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
                                Endpoint = (string)reader["endpoint"]
                            }
                        );
                    }
                }
            }

            return subscriptions;
        }

        /// <summary>
        /// Checks if the parent container exists for a subscription in a specific application.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <returns>The count of matching parent containers (0 if none found).</returns>
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

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmdCheckParent = new SqlCommand(sqlCheckParent, conn))
            {
                cmdCheckParent.Parameters.AddWithValue("@applicationName", appName);
                cmdCheckParent.Parameters.AddWithValue("@containerName", containerName);

                conn.Open();
                containerCount = (int)cmdCheckParent.ExecuteScalar();
            }

            return containerCount;
        }

        /// <summary>
        /// Checks if a subscription already exists in a specific application and container.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="subName">The name of the subscription.</param>
        /// <returns>The count of matching subscriptions (0 if none found).</returns>
        public int CheckIfSubscriptionAlreadyExists(string appName, string containerName, string subName)
        {
            int subCount = 0;

            string sqlCheckDuplicate =
                @"
            SELECT COUNT(*)
            FROM [subscription]
            WHERE [resource-name] = @subName
              AND [container-resource-name] = @containerName
              AND [application-resource-name] = @appName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmdCheckDuplicate = new SqlCommand(sqlCheckDuplicate, conn))
            {
                cmdCheckDuplicate.Parameters.AddWithValue("@subName", subName);
                cmdCheckDuplicate.Parameters.AddWithValue("@containerName", containerName);
                cmdCheckDuplicate.Parameters.AddWithValue("@appName", appName);

                conn.Open();
                subCount = (int)cmdCheckDuplicate.ExecuteScalar();
            }

            return subCount;
        }

        /// <summary>
        /// Inserts a new subscription into the database for a specific application and container.
        /// </summary>
        /// <param name="resourceName">The name of the subscription resource.</param>
        /// <param name="creationTimeDate">The creation datetime of the subscription.</param>
        /// <param name="containerName">The name of the container where the subscription belongs.</param>
        /// <param name="appName">The name of the application where the subscription belongs.</param>
        /// <param name="evt">The event code associated with the subscription.</param>
        /// <param name="endpoint">The endpoint URL for the subscription.</param>
        /// <returns>The number of rows affected by the insert operation.</returns>
        public int InsertNewSubscription(
            string resourceName,
            DateTime creationTimeDate,
            string containerName,
            string appName,
            int evt,
            string endpoint
        )
        {
            int rowsAffected = 0;

            string sqlInsert =
                @"
            INSERT INTO [subscription]
            ([resource-name], [creation-datetime], [container-resource-name], [application-resource-name], [evt], [endpoint])
            VALUES (@resourceName, @creationDatetime, @containerResourceName, @appName, @evt, @endpoint)
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sqlInsert, conn))
            {
                cmd.Parameters.AddWithValue("@resourceName", resourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", creationTimeDate);
                cmd.Parameters.AddWithValue("@containerResourceName", containerName);
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@evt", evt);
                cmd.Parameters.AddWithValue("@endpoint", endpoint);

                conn.Open();
                rowsAffected = cmd.ExecuteNonQuery();
            }

            return rowsAffected;
        }


        /// <summary>
        /// Retrieves a single application by its resource name.
        /// </summary>
        /// <param name="appName">The name of the application to retrieve.</param>
        /// <returns>
        /// An <see cref="Application"/> object if found; otherwise, <c>null</c>.
        /// </returns>
        public Application GetApplication(string appName)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(
                @"
            SELECT [resource-name],
                   [creation-datetime]
            FROM [application]
            WHERE [resource-name] = @appName
        ",
                conn
            ))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
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


        /// <summary>
        /// Retrieves the SOMOID resource type for a given application.
        /// Since applications always have a fixed resource type of "application",
        /// this method returns the constant if the application exists.
        /// </summary>
        /// <param name="appName">The name of the application.</param>
        /// <returns>
        /// The string "application" if the application exists; otherwise, <c>null</c>.
        /// </returns>
        [Obsolete("GetApplicationResTypeValue is deprecated and will be removed in a future release. Resource type is now inferred from URL path.")]

        public string GetApplicationResTypeValue(string appName)
        {
            string sql =
                @"
            SELECT [resource-name]
            FROM [application]
            WHERE [resource-name] = @appName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);

                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                {
                    return null; // Application not found
                }

                // SOMOID rule: application resource type is always "application"
                return "application";
            }
        }


        /// <summary>
        /// Inserts a new application into the database.
        /// </summary>
        /// <param name="value">The application model to insert.</param>
        /// <returns>True if the insert succeeded, otherwise false.</returns>
        public bool InsertApplication(Application value)
        {
            string sql =
                @"
            INSERT INTO [application]
                ([resource-name], [creation-datetime])
            VALUES (@resourceName, @creationDatetime)
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }


        /// <summary>
        /// Renames an existing application by updating its resource name.
        /// </summary>
        /// <param name="currentName">The current name of the application.</param>
        /// <param name="newName">The new name to assign to the application.</param>
        /// <returns>
        /// The updated <see cref="Application"/> object if the operation succeeds;  
        /// otherwise, <c>null</c> if the application does not exist.
        /// </returns>
        public Application RenameApplication(string currentName, string newName)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                // Update name
                using (SqlCommand cmd = new SqlCommand(
                    @"
                UPDATE [application]
                SET [resource-name] = @newName
                WHERE [resource-name] = @currentName
            ",
                    conn
                ))
                {
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@currentName", currentName);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        return null; // nothing updated — application not found
                    }
                }

                // Fetch updated row
                using (SqlCommand cmdGet = new SqlCommand(
                    @"
                SELECT [resource-name],
                       [creation-datetime]
                FROM [application]
                WHERE [resource-name] = @appName
            ",
                    conn
                ))
                {
                    cmdGet.Parameters.AddWithValue("@appName", newName);

                    using (SqlDataReader reader = cmdGet.ExecuteReader())
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

        /// <summary>
        /// Permanently deletes an application from the database by its resource name.
        /// </summary>
        /// <param name="appName">The name of the application to delete.</param>
        /// <returns>True if the application was deleted; otherwise, false.</returns>
        public bool HardDeleteApplication(string appName)
        {
            string sql =
                @"
            DELETE FROM [application]
            WHERE [resource-name] = @appName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }


        /// <summary>
        /// Checks whether an application with the specified name exists in the database.
        /// </summary>
        /// <param name="appName">The name of the application to check.</param>
        /// <returns>True if the application exists; otherwise, false.</returns>
        public bool ApplicationExists(string appName)
        {
            string sql =
                @"
            SELECT COUNT(*)
            FROM [application]
            WHERE [resource-name] = @appName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);

                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }


        /// <summary>
        /// Retrieves a single container by its name within a specific application.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container to retrieve.</param>
        /// <returns>
        /// The <see cref="Container"/> object if found; otherwise, <c>null</c>.
        /// </returns>
        public Container GetContainer(string appName, string containerName)
        {
            string sql =
                @"
            SELECT c.[resource-name],
                   c.[creation-datetime],
                   c.[application-resource-name]
            FROM [container] c
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
              AND c.[resource-name] = @containerName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
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


        /// <summary>
        /// Checks whether a container exists within a specific application.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container to check.</param>
        /// <returns>True if the container exists; otherwise, false.</returns>
        public bool ContainerExists(string appName, string containerName)
        {
            string sql =
                @"
            SELECT COUNT(*)
            FROM [container] c
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
              AND c.[resource-name] = @containerName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);

                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }


        /// <summary>
        /// Checks whether a container with the specified name exists in a given application.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container to check.</param>
        /// <returns>True if the container exists; otherwise, false.</returns>
        public bool ContainerNameExists(string appName, string containerName)
        {
            string sql =
                @"
            SELECT COUNT(*)
            FROM [container]
            WHERE [resource-name] = @containerName
              AND [application-resource-name] = @appName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@appName", appName);

                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }


        /// <summary>
        /// Inserts a new container into a specific application.
        /// </summary>
        /// <param name="value">The container model to insert.</param>
        /// <returns>True if the insert succeeded; otherwise, false.</returns>
        public bool InsertContainer(Container value)
        {
            string sql =
                @"
            INSERT INTO [container]
                ([resource-name], [creation-datetime], [application-resource-name])
            VALUES (@resourceName, @creationDatetime, @appResourceName)
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                cmd.Parameters.AddWithValue("@appResourceName", value.ApplicationResourceName);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }


        /// <summary>
        /// Renames an existing container within a specific application.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The current name of the container.</param>
        /// <param name="newName">The new name to assign to the container.</param>
        /// <returns>
        /// The updated <see cref="Container"/> object if the operation succeeds;  
        /// otherwise, <c>null</c> if the container does not exist.
        /// </returns>
        public Container RenameContainer(string appName, string containerName, string newName)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                // Update container name
                using (SqlCommand cmd = new SqlCommand(
                    @"
                UPDATE [container]
                SET [resource-name] = @newName
                WHERE [resource-name] = @oldName
                  AND [application-resource-name] = @appName
            ",
                    conn
                ))
                {
                    cmd.Parameters.AddWithValue("@newName", newName);
                    cmd.Parameters.AddWithValue("@oldName", containerName);
                    cmd.Parameters.AddWithValue("@appName", appName);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                    {
                        return null; // container not found
                    }
                }

                // Retrieve updated container
                using (SqlCommand cmdGet = new SqlCommand(
                    @"
                SELECT [resource-name],
                       [creation-datetime],
                       [application-resource-name]
                FROM [container]
                WHERE [resource-name] = @name
                  AND [application-resource-name] = @appName
            ",
                    conn
                ))
                {
                    cmdGet.Parameters.AddWithValue("@name", newName);
                    cmdGet.Parameters.AddWithValue("@appName", appName);

                    using (SqlDataReader reader = cmdGet.ExecuteReader())
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
            }

            return null;
        }


        /// <summary>
        /// Deletes a container and all its related content instances and subscriptions.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container to delete.</param>
        /// <returns>True if the container was deleted; otherwise, false.</returns>
        public bool DeleteContainerCascade(string appName, string containerName)
        {
            using (SqlConnection conn = new SqlConnection(connection))
            {
                conn.Open();

                // Delete all content instances in the container
                using (SqlCommand cmdDelCI = new SqlCommand(
                    @"
                DELETE ci
                FROM [content-instance] ci
                JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                                     AND c.[application-resource-name] = ci.[application-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND ci.[application-resource-name] = @appName
            ",
                    conn
                ))
                {
                    cmdDelCI.Parameters.AddWithValue("@appName", appName);
                    cmdDelCI.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelCI.ExecuteNonQuery();
                }

                // Delete all subscriptions in the container
                using (SqlCommand cmdDelSub = new SqlCommand(
                    @"
                DELETE s
                FROM [subscription] s
                JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                                     AND c.[application-resource-name] = s.[application-resource-name]
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
                  AND s.[application-resource-name] = @appName
            ",
                    conn
                ))
                {
                    cmdDelSub.Parameters.AddWithValue("@appName", appName);
                    cmdDelSub.Parameters.AddWithValue("@containerName", containerName);
                    cmdDelSub.ExecuteNonQuery();
                }

                // Delete the container itself
                using (SqlCommand cmdDelContainer = new SqlCommand(
                    @"
                DELETE c
                FROM [container] c
                JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
                WHERE a.[resource-name] = @appName
                  AND c.[resource-name] = @containerName
            ",
                    conn
                ))
                {
                    cmdDelContainer.Parameters.AddWithValue("@appName", appName);
                    cmdDelContainer.Parameters.AddWithValue("@containerName", containerName);

                    return cmdDelContainer.ExecuteNonQuery() > 0;
                }
            }
        }

        /// <summary>
        /// Checks whether the parent container exists for a content instance.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container to check.</param>
        /// <returns>
        /// <c>true</c> if the parent container exists;  
        /// <c>false</c> if it does not exist.
        /// </returns>
        public bool ContentInstanceParentExists(string appName, string containerName)
        {
            string sql =
                @"
            SELECT COUNT(*)
            FROM [container] c
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
              AND c.[resource-name] = @containerName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);

                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }

        /// <summary>
        /// Checks whether a specific content instance exists within a given container and application.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="ciName">The name of the content instance to check.</param>
        /// <returns>
        /// <c>true</c> if the content instance exists;  
        /// <c>false</c> if it does not exist.
        /// </returns>
        public bool ContentInstanceExistsInContainer(string appName, string containerName, string ciName)
        {
            string sql =
                @"
            SELECT COUNT(*)
            FROM [content-instance]
            WHERE [resource-name] = @ciName
              AND [container-resource-name] = @containerName
              AND [application-resource-name] = @appName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ciName", ciName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@appName", appName);

                conn.Open();
                return (int)cmd.ExecuteScalar() > 0;
            }
        }


        /// <summary>
        /// Retrieves a specific content instance within a given container and application.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container.</param>
        /// <param name="ciName">The name of the content instance to retrieve.</param>
        /// <returns>
        /// The <see cref="ContentInstance"/> object if found;  
        /// otherwise, <c>null</c> if the content instance does not exist.
        /// </returns>
        public ContentInstance GetContentInstance(string appName, string containerName, string ciName)
        {
            string sql =
                @"
            SELECT ci.[resource-name],
                   ci.[creation-datetime],
                   ci.[container-resource-name],
                   ci.[application-resource-name],
                   ci.[content-type],
                   ci.[content]
            FROM [content-instance] ci
            JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                                AND c.[application-resource-name] = ci.[application-resource-name]
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
              AND c.[resource-name] = @containerName
              AND ci.[resource-name] = @ciName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@ciName", ciName);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new ContentInstance
                        {
                            ResourceName = (string)reader["resource-name"],
                            CreationDatetime = (DateTime)reader["creation-datetime"],
                            ContainerResourceName = (string)reader["container-resource-name"],
                            ApplicationResourceName = (string)reader["application-resource-name"],
                            ContentType = (string)reader["content-type"],
                            Content = (string)reader["content"],
                        };
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Inserts a new content instance into a specific container and application.
        /// </summary>
        /// <param name="value">The <see cref="ContentInstance"/> object to insert.</param>
        /// <returns>
        /// <c>true</c> if the insertion succeeds;  
        /// <c>false</c> if it fails.
        /// </returns>
        public bool InsertContentInstance(ContentInstance value)
        {
            string sql =
                @"
            INSERT INTO [content-instance]
                ([resource-name], [creation-datetime], [container-resource-name], [application-resource-name],
                 [content-type], [content])
            VALUES (@resourceName, @creationDatetime, @containerResourceName, @appResourceName,
                    @contentType, @content)
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@resourceName", value.ResourceName);
                cmd.Parameters.AddWithValue("@creationDatetime", value.CreationDatetime);
                cmd.Parameters.AddWithValue("@containerResourceName", value.ContainerResourceName);
                cmd.Parameters.AddWithValue("@appResourceName", value.ApplicationResourceName);
                cmd.Parameters.AddWithValue("@contentType", value.ContentType);
                cmd.Parameters.AddWithValue("@content", value.Content);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }


        /// <summary>
        /// Deletes a specific content instance within a given container and application.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container containing the content instance.</param>
        /// <param name="ciName">The name of the content instance to delete.</param>
        /// <returns>
        /// <c>true</c> if the content instance was deleted;  
        /// <c>false</c> if it did not exist.
        /// </returns>
        public bool DeleteContentInstance(string appName, string containerName, string ciName)
        {
            string sql =
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
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@ciName", ciName);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Retrieves all content instances belonging to a specific application.
        /// </summary>
        /// <param name="appName">The name of the application whose content instances are being retrieved.</param>
        /// <returns>
        /// A list of paths to all content instances in the format:
        /// <c>/api/somiod/{appName}/{containerName}/{ciName}</c>.
        /// </returns>
        public List<string> GetAllContentInstancesFromApp(string appName)
        {
            var paths = new List<string>();

            string sql =
                @"
            SELECT c.[resource-name] AS contName,
                   ci.[resource-name] AS ciName
            FROM [content-instance] ci
            JOIN [container] c ON c.[resource-name] = ci.[container-resource-name]
                                AND c.[application-resource-name] = ci.[application-resource-name]
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
            ORDER BY c.[resource-name], ci.[creation-datetime]
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string containerName = (string)reader["contName"];
                        string ciName = (string)reader["ciName"];
                        paths.Add($"/api/somiod/{appName}/{containerName}/{ciName}");
                    }
                }
            }

            return paths;
        }


        /// <summary>
        /// Deletes a specific subscription within a given container and application.
        /// </summary>
        /// <param name="appName">The name of the application containing the container.</param>
        /// <param name="containerName">The name of the container containing the subscription.</param>
        /// <param name="subName">The name of the subscription to delete.</param>
        /// <returns>
        /// <c>true</c> if the subscription was deleted;  
        /// <c>false</c> if it did not exist.
        /// </returns>
        public bool DeleteSubscription(string appName, string containerName, string subName)
        {
            string sql =
                @"
            DELETE s
            FROM [subscription] s
            JOIN [container] c ON c.[resource-name] = s.[container-resource-name]
                                 AND c.[application-resource-name] = s.[application-resource-name]
            JOIN [application] a ON a.[resource-name] = c.[application-resource-name]
            WHERE a.[resource-name] = @appName
              AND c.[resource-name] = @containerName
              AND s.[resource-name] = @subName
        ";

            using (SqlConnection conn = new SqlConnection(connection))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@appName", appName);
                cmd.Parameters.AddWithValue("@containerName", containerName);
                cmd.Parameters.AddWithValue("@subName", subName);

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
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
