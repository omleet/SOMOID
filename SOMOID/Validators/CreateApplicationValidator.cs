using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SOMOID.Models;
using SOMOID.Validators;

namespace SOMOID.Validators
{
    public class CreateApplicationValidator : IValidator<Application>
    {
        public List<ValidationError> Validate(Application value)
        {
            var errors = new List<ValidationError>();

            if (value == null)
            {
                errors.Add(
                    new ValidationError
                    {
                        Field = null,
                        Message = "O corpo da requisição não pode estar vazio",
                    }
                );
                return errors;
            }

            // to avoid having this on the controller logic, this will do the same, but in the validator

            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName = GenerateResourceName();


            return errors;
        }
    
    
        private string GenerateResourceName()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var suffix = Guid.NewGuid().ToString().Substring(0, 8);
            return $"app-{timestamp}-{suffix}";
        }
    }
}
