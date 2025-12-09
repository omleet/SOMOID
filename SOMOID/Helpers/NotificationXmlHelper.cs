using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using SOMOID.Models;

namespace SOMOID.Helpers
{
    public static class NotificationXmlHelper
    {
        private static readonly Lazy<XmlSchemaSet> SchemaSet = new Lazy<XmlSchemaSet>(LoadSchema, LazyThreadSafetyMode.ExecutionAndPublication);

        public static string SerializeAndSave(NotificationPayload payload, string appName)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }

            if (string.IsNullOrWhiteSpace(appName))
            {
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(appName));
            }

            var document = BuildDocument(payload);
            Validate(document);
            var directory = GetApplicationDirectory(appName);
            Directory.CreateDirectory(directory);
            var fileName = $"{payload.EventType}-{DateTime.UtcNow:yyyyMMddTHHmmssfff}-{Guid.NewGuid():N}.xml";
            var path = Path.Combine(directory, fileName);

            using (var writer = XmlWriter.Create(path, new XmlWriterSettings { Encoding = Encoding.UTF8, Indent = true }))
            {
                document.WriteTo(writer);
            }

            return path;
        }

        private static XDocument BuildDocument(NotificationPayload payload)
        {
            var notificationElement = new XElement("notification",
                new XElement("eventType", payload.EventType ?? string.Empty),
                new XElement("eventCode", payload.EventCode),
                new XElement("subscription",
                    new XElement("resourceName", payload.Subscription?.ResourceName ?? string.Empty),
                    new XElement("evt", payload.Subscription?.Evt ?? 0),
                    new XElement("endpoint", payload.Subscription?.Endpoint ?? string.Empty)
                ),
                new XElement("resource",
                    new XElement("resourceName", payload.Resource?.ResourceName ?? string.Empty),
                    new XElement("creationDatetime", payload.Resource?.CreationDatetime ?? string.Empty),
                    new XElement("resType", payload.Resource?.ResType ?? string.Empty),
                    new XElement("containerResourceName", payload.Resource?.ContainerResourceName ?? string.Empty),
                    new XElement("applicationResourceName", payload.Resource?.ApplicationResourceName ?? string.Empty),
                    new XElement("contentType", payload.Resource?.ContentType ?? string.Empty),
                    new XElement("content", payload.Resource?.Content ?? string.Empty),
                    new XElement("path", payload.Resource?.Path ?? string.Empty)
                ),
                new XElement("triggeredAt", payload.TriggeredAt ?? string.Empty)
            );

            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), notificationElement);
        }

        private static void Validate(XDocument document)
        {
            var errors = new List<string>();

            document.Validate(SchemaSet.Value, (sender, args) =>
            {
                errors.Add(args.Message);
            }, true);

            if (errors.Any())
            {
                throw new InvalidOperationException(string.Join("; ", errors));
            }
        }

        private static XmlSchemaSet LoadSchema()
        {
            var schemaPath = Path.Combine(GetAppDataDirectory(), "NotificationSchema.xsd");

            if (!File.Exists(schemaPath))
            {
                throw new FileNotFoundException("Notification schema not found.", schemaPath);
            }

            var schemaSet = new XmlSchemaSet();

            using (var stream = File.OpenRead(schemaPath))
            {
                schemaSet.Add(string.Empty, XmlReader.Create(stream));
            }

            return schemaSet;
        }

        private static string GetAppDataDirectory()
        {
            var mapped = HostingEnvironment.MapPath("~/App_Data");
            if (!string.IsNullOrWhiteSpace(mapped))
            {
                return mapped;
            }

            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data");
        }

        private static string GetNotificationsRoot()
        {
            return Path.Combine(GetAppDataDirectory(), "Notifications");
        }

        private static string GetApplicationDirectory(string appName)
        {
            var sanitized = SanitizeName(appName);
            return Path.Combine(GetNotificationsRoot(), sanitized);
        }

        private static string SanitizeName(string value)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);

            foreach (var ch in value)
            {
                builder.Append(invalidChars.Contains(ch) ? '_' : ch);
            }

            return builder.ToString();
        }
    }
}
