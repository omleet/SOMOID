using SOMOID.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Validators
{
    /// <summary>
    /// Validator for <see cref="Subscription"/> objects.
    /// Ensures required fields are present, validates the endpoint format, and auto-generates <c>ResourceName</c> if missing.
    /// Inspired by <see href="https://express-validator.github.io/docs/">express-validator</see>.
    /// </summary>
    public class SubscriptionValidator : IValidator<Subscription>
    {
        public List<ValidationError> Validate(Subscription value)
        {
            var errors = new List<ValidationError>();

            if (value == null)
            {
                errors.Add(new ValidationError { Field = null, Message = "O corpo da requisição não pode estar vazio." });
                return errors;
            }

            // Auto-generate ResourceName if missing
            if (string.IsNullOrWhiteSpace(value.ResourceName))
            {
                value.ResourceName =
                    "sub-" +
                    DateTime.UtcNow.ToString("yyyyMMddHHmmss") +
                    "-" +
                    Guid.NewGuid().ToString().Substring(0, 8);
            }

            // Validate evt
            if (value.Evt != 1 && value.Evt != 2 && value.Evt != 3)
                errors.Add(new ValidationError { Field = "evt", Message = "O campo 'evt' deve ser 1 (criação), 2 (deletion) ou 3 (ambos)." });

            // Validate endpoint presence
            if (string.IsNullOrWhiteSpace(value.Endpoint))
                errors.Add(new ValidationError { Field = "endpoint", Message = "O campo 'endpoint' é obrigatório." });

            // Validate endpoint URL
            if (!IsValidEndpoint(value.Endpoint))
                errors.Add(new ValidationError { Field = "endpoint", Message = "O 'endpoint' deve ser uma URL válida (http://, https:// ou mqtt://)." });

            return errors;
        }

        /// <summary>
        /// Checks if a string is a valid endpoint (HTTP, HTTPS, or MQTT).
        /// </summary>
        /// <param name="endpoint">The string to validate.</param>
        /// <returns>True if the endpoint is valid; otherwise, false.</returns>
        private bool IsValidEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return false;

            try
            {
                if (
                    endpoint.StartsWith("http://")
                    || endpoint.StartsWith("https://")
                    || endpoint.StartsWith("mqtt://")
                )
                {
                    var uri = new Uri(endpoint);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
