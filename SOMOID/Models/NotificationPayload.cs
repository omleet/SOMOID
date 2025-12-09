using Newtonsoft.Json;

namespace SOMOID.Models
{
    public class NotificationPayload
    {
        [JsonProperty("eventType")]
        public string EventType { get; set; }

        [JsonProperty("eventCode")]
        public int EventCode { get; set; }

        [JsonProperty("subscription")]
        public NotificationSubscriptionInfo Subscription { get; set; }

        [JsonProperty("resource")]
        public NotificationResourceInfo Resource { get; set; }

        [JsonProperty("triggeredAt")]
        public string TriggeredAt { get; set; }
    }

    public class NotificationSubscriptionInfo
    {
        [JsonProperty("resourceName")]
        public string ResourceName { get; set; }

        [JsonProperty("evt")]
        public int Evt { get; set; }

        [JsonProperty("endpoint")]
        public string Endpoint { get; set; }
    }

    public class NotificationResourceInfo
    {
        [JsonProperty("resourceName")]
        public string ResourceName { get; set; }

        [JsonProperty("creationDatetime")]
        public string CreationDatetime { get; set; }

        [JsonProperty("resType")]
        public string ResType { get; set; }

        [JsonProperty("containerResourceName")]
        public string ContainerResourceName { get; set; }

        [JsonProperty("applicationResourceName")]
        public string ApplicationResourceName { get; set; }

        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }
    }
}
