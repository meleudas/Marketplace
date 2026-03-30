using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Marketplace.Domain.Users.ValueObjects
{
    public record Email : ValueObject
    {
        public string Value { get; init; }

        private Email(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Email cannot be empty");

            if (!IsValidEmail(value))
                throw new DomainException("Invalid email format");

            Value = value.ToLower().Trim();
        }

        public static Email Create(string value) => new(value);

        private static bool IsValidEmail(string email)
        {
            var pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern);
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
