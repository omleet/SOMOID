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


        /// <summary>
        /// Valida se uma string é um endpoint válido (HTTP ou MQTT).
        /// </summary>
        /// <param name="endpoint">String a validar</param>
        /// <returns>true se é um endpoint válido, false caso contrário</returns>
        private bool IsValidEndpoint(string endpoint)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                return false;

            try
            {
                // Verificar se começa com http://, https:// ou mqtt://
                if (
                    endpoint.StartsWith("http://")
                    || endpoint.StartsWith("https://")
                    || endpoint.StartsWith("mqtt://")
                )
                {
                    // Tentar fazer parse como URI
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