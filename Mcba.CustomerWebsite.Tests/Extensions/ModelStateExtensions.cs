using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Mcba.CustomerWebsite.Tests.Extensions;

// From the W10 lectorial: runs the data annotation validation MVC runs before an action.
public static class ModelStateExtensions
{
    extension(ModelStateDictionary modelState)
    {
        public void ValidateObject<T>(T instance)
        {
            var context = new ValidationContext(instance);
            var results = new List<ValidationResult>();

            Validator.TryValidateObject(instance, context, results, validateAllProperties: true);

            foreach(var result in results)
            {
                foreach(var member in result.MemberNames.DefaultIfEmpty(string.Empty))
                {
                    modelState.AddModelError(member, result.ErrorMessage!);
                }
            }
        }
    }
}