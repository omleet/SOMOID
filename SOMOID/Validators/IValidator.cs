using System;
using System.Collections.Generic;
using System.Linq;
using SOMOID.Models;
using System.Web;

namespace SOMOID.Validators
{
    /// <summary>
    /// Defines a generic validator for objects of type <typeparamref name="T"/>.
    /// Inspired by <see href="https://express-validator.github.io/docs/">express-validator</see>,
    /// this interface standardizes validation logic by returning a list of <see cref="ValidationError"/> objects.
    /// 
    /// Implementations should perform validation on the provided object and return
    /// all errors found. If the object is valid, an empty list should be returned.
    /// </summary>
    /// <typeparam name="T">The type of object to validate.</typeparam>
    public interface IValidator<T>
    {
        /// <summary>
        /// Validates the specified object and returns a list of errors.
        /// </summary>
        /// <param name="value">The object to validate.</param>
        /// <returns>
        /// A list of <see cref="ValidationError"/> instances describing validation failures.
        /// If the object is valid, the list will be empty.
        /// </returns>
        List<ValidationError> Validate(T value);
    }
}
