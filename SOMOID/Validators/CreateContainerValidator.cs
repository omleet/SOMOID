using SOMOID.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Validators
{
    /// <summary>
    /// Validator for <see cref="Container"/> when creating a new instance.
    /// Generates a resource name if missing. Inspired by <see href="https://express-validator.github.io/docs/">express-validator</see>.
    /// </summary>
    public class CreateContainerValidator : IValidator<Container>
    {
        public List<ValidationError> Validate(Container value)
        {
            var errors = new List<ValidationError>();

            if (value == null)
            {
                errors.Add(new ValidationError { Field = null, Message = "O corpo da requisição não pode estar vazio." });
                return errors;
            }

            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName = GeneratePropertyName();

            return errors;
        }

        private string GeneratePropertyName()
        {
            return "cont-"
                    + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                    + "-"
                    + Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}
