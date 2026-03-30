using Marketplace.Domain.Common.Models;

namespace Marketplace.Domain.Users.ValueObjects
{
    public record IdentityUserId : ValueObject
    {
        public Guid Value { get; }

        private IdentityUserId(Guid value) => Value = value;

        public static IdentityUserId New() => new(Guid.NewGuid());
        public static IdentityUserId From(Guid value) => new(value);

        public static implicit operator Guid(IdentityUserId id) => id.Value;
        public static implicit operator IdentityUserId(Guid value) => From(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
