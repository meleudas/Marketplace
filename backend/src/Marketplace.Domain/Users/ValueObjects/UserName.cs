using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;


namespace Marketplace.Domain.Users.ValueObjects
{
    public record UserName : ValueObject
    {
        public string Value { get; init; }

        private UserName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException("Username cannot be empty");

            if (value.Length < 3 || value.Length > 50)
                throw new DomainException("Username must be between 3 and 50 characters");

            Value = value.Trim();
        }

        public static UserName Create(string value) => new(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
