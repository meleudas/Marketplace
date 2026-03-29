using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;


namespace Marketplace.Domain.Users.ValueObjects
{
    public record PhoneNumber : ValueObject
    {
        public string? Value { get; init; }
        public bool IsVerified { get; init; }

        private PhoneNumber(string? value, bool isVerified)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var cleaned = new string(value.Where(char.IsDigit).ToArray());
                if (cleaned.Length < 10)
                    throw new DomainException("Invalid phone number");
                Value = $"+{cleaned}";
            }
            IsVerified = isVerified;
        }

        public static PhoneNumber Create(string? value, bool isVerified = false)
            => new(value, isVerified);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value ?? string.Empty;
            yield return IsVerified;
        }
    }
}
