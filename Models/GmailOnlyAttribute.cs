using System;
using System.ComponentModel.DataAnnotations;

namespace GymCrudApp.Models
{
    public class GmailOnlyAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is string email && !string.IsNullOrWhiteSpace(email))
            {
                var trimmed =  email.Trim();

                if (trimmed.Length > "@gmail.com".Length && trimmed.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase))
                {
                    return ValidationResult.Success;
                }
                
                return new ValidationResult(ErrorMessage ?? "Invalid email address. Please provide a valid email.");
            }
            
            return ValidationResult.Success;
        }
    }
}

