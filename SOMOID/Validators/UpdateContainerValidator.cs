using SOMOID.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SOMOID.Validators
{
    /// <summary>
    /// Validator responsible for validating <see cref="Container"/> objects during update operations.
    /// </summary>
    public class UpdateContainerValidator : IValidator<Container>
    {

        /// <summary>
        /// Validates the specified <see cref="Container"/> object.
        /// </summary>
        /// <param name="value">The container instance to validate.</param>
        /// <returns>
        /// A list of <see cref="ValidationError"/> objects representing validation errors.
        /// If the container is valid, the list will be empty.
        /// </returns>
        /// <remarks>
        /// Validation rules:
        /// <list type="bullet">
        /// <item><description>The container cannot be null.</description></item>
        /// <item><description>The <c>ResourceName</c> property is required.</description></item>
        /// </list>
        /// </remarks>
        public List<ValidationError> Validate(Container value)
        {
            var errors = new List<ValidationError>();

            if (value == null)
            {
                errors.Add(new ValidationError { Field = null, Message = "O corpo da requisição não pode estar vazio." });
                return errors;
            }

            if (string.IsNullOrWhiteSpace(value.ResourceName))
            {
                errors.Add(new ValidationError
                {
                    Field = "resourceName",
                    Message = "O campo 'resourceName' é obrigatório para atualizar o container."
                });
                return errors;
            }

            return errors;

        }
    }
}