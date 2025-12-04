using SOMOID.Models;
using SOMOID.Validators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Validators
{
    /// <summary>
    /// Validator for updating <see cref="Application"/> objects.
    /// Ensures required fields are present for updates.
    /// Inspired by <see href="https://express-validator.github.io/docs/">express-validator</see>.
    /// </summary>
    public class UpdateApplicationValidator : IValidator<Application>
    {
        public List<ValidationError> Validate(Application value)
        {
            var errors = new List<ValidationError>();

            if (value == null)
            {
                errors.Add(new ValidationError { Field = null, Message = "O corpo da requisição não pode estar vazio" });
                return errors;
            }

            if (string.IsNullOrWhiteSpace(value.ResourceName))
            {
                errors.Add(new ValidationError
                {
                    Field = "resourceName",
                    Message = "O campo 'resourceName' é obrigatório para atualizar uma application."
                });
                return errors;
            }

            return errors;
        }
    }
}
