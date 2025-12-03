using SOMOID.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SOMOID.Models;

namespace SOMOID.Validators
{
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

        private bool IsValidEndpoint(string endpoint)
        {
            return Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == "mqtt");
        }
    }

}