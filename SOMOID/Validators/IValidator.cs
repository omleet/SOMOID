using System;
using System.Collections.Generic;
using System.Linq;
using SOMOID.Models;
using System.Web;

namespace SOMOID.Validators
{
	public interface IValidator<T>
	{
		/// <summary>
		/// Validates the object and returns a list of errors (empty if valid)
		/// </summary>
		List<ValidationError> Validate(T value);
	}
}