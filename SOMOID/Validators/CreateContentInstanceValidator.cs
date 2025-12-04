using SOMOID.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace SOMOID.Validators
{
    public class CreateContentInstanceValidator : IValidator<ContentInstance>
    {
        public List<ValidationError> Validate(ContentInstance value)
        {
            var errors = new List<ValidationError>();

            if (value == null)
            {
                errors.Add(new ValidationError
                {
                    Field = null,
                    Message = "O corpo da requisição não pode estar vazio."
                });
                return errors;
            }

            if (string.IsNullOrWhiteSpace(value.ContentType))
            {
                errors.Add(new ValidationError
                {
                    Field = "contentType",
                    Message = "O campo 'contentType' é obrigatório."
                });
                return errors;
            }
                
            if (string.IsNullOrWhiteSpace(value.Content))
            {
                errors.Add(new ValidationError
                {
                    Field = "contentType",
                    Message = "O campo 'contentType' é obrigatório."
                });
                return errors;
            }

            // lets move this after all the checks pass, no need to generate a resourcename if theres a probability that it wont be used 
            
            if (string.IsNullOrWhiteSpace(value.ResourceName))
                value.ResourceName = GenerateResourceName();

            return errors;
        }

        private string GenerateResourceName()
        {
            return "ci-"
                    + DateTime.UtcNow.ToString("yyyyMMddHHmmss")
                    + "-"
                    + Guid.NewGuid().ToString().Substring(0, 8);
        }
    }
}