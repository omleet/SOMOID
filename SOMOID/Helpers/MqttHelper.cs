using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SOMOID.Helpers
{
    /// <summary>
    /// Helper class for managing MQTT connections and publishing notifications.
    /// Handles connection pooling and message publishing to MQTT brokers.
    /// </summary>
    public class MqttHelper
    {
        private static readonly object LockObject = new object();
        private static readonly Dictionary<string, MqttClient> ClientPool = new Dictionary<string, MqttClient>();
        
        private const string DefaultBroker = "localhost";
        private const int DefaultPort = 1883;
        private const byte QosLevel = MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE; // QoS 1

        /// <summary>
        /// Publishes a notification message to an MQTT broker.
        /// </summary>
        /// <param name="brokerEndpoint">MQTT broker endpoint in format: mqtt://host:port</param>
        /// <param name="topic">MQTT topic to publish to (e.g., api/somiod/app1/container1)</param>
        /// <param name="payload">JSON payload to send</param>
        /// <returns>True if published successfully, false otherwise</returns>
        public static bool PublishNotification(string brokerEndpoint, string topic, string payload)
        {
            try
            {
                var (host, port) = ParseMqttEndpoint(brokerEndpoint);
                var client = GetOrCreateClient(host, port);

                if (client == null || !client.IsConnected)
                {
                    Debug.WriteLine($"MQTT client not connected to {host}:{port}");
                    return false;
                }

                // Publish message with QoS 1 (at least once delivery)
                client.Publish(
                    topic,
                    Encoding.UTF8.GetBytes(payload),
                    QosLevel,
                    retain: false
                );

                Debug.WriteLine($"MQTT notification published to {host}:{port} on topic '{topic}'");
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to publish MQTT notification: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parses an MQTT endpoint string into host and port components.
        /// </summary>
        /// <param name="endpoint">Endpoint in format: mqtt://host:port or mqtt://host</param>
        /// <returns>Tuple of (host, port)</returns>
        private static (string host, int port) ParseMqttEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                return (DefaultBroker, DefaultPort);
            }

            try
            {
                // Remove mqtt:// prefix if present
                var cleanEndpoint = endpoint.Trim();
                if (cleanEndpoint.StartsWith("mqtt://", StringComparison.OrdinalIgnoreCase))
                {
                    cleanEndpoint = cleanEndpoint.Substring(7);
                }

                // Parse host and port
                var parts = cleanEndpoint.Split(':');
                var host = parts[0];
                var port = parts.Length > 1 && int.TryParse(parts[1], out int parsedPort) 
                    ? parsedPort 
                    : DefaultPort;

                return (host, port);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to parse MQTT endpoint '{endpoint}': {ex.Message}. Using defaults.");
                return (DefaultBroker, DefaultPort);
            }
        }

        /// <summary>
        /// Gets an existing MQTT client from the pool or creates a new one.
        /// </summary>
        /// <param name="host">MQTT broker host</param>
        /// <param name="port">MQTT broker port</param>
        /// <returns>Connected MqttClient or null if connection fails</returns>
        private static MqttClient GetOrCreateClient(string host, int port)
        {
            var key = $"{host}:{port}";

            lock (LockObject)
            {
                // Check if we have a connected client in the pool
                if (ClientPool.ContainsKey(key))
                {
                    var existingClient = ClientPool[key];
                    if (existingClient.IsConnected)
                    {
                        return existingClient;
                    }
                    
                    // Remove disconnected client
                    ClientPool.Remove(key);
                }

                // Create new client
                try
                {
                    var client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
                    
                    // Generate unique client ID
                    var clientId = $"SOMOID_{Guid.NewGuid():N}";
                    
                    // Connect to broker
                    client.Connect(clientId);

                    if (client.IsConnected)
                    {
                        ClientPool[key] = client;
                        Debug.WriteLine($"MQTT client connected to {host}:{port}");
                        return client;
                    }
                    else
                    {
                        Debug.WriteLine($"Failed to connect MQTT client to {host}:{port}");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating MQTT client for {host}:{port}: {ex.Message}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Disconnects all MQTT clients in the pool.
        /// Should be called on application shutdown.
        /// </summary>
        public static void DisconnectAll()
        {
            lock (LockObject)
            {
                foreach (var client in ClientPool.Values)
                {
                    try
                    {
                        if (client.IsConnected)
                        {
                            client.Disconnect();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error disconnecting MQTT client: {ex.Message}");
                    }
                }
                
                ClientPool.Clear();
            }
        }
    }
}
