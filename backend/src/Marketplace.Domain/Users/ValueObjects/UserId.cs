using Marketplace.Domain.Common.Models;
using System;
using System.Collections.Generic;
using System.Text;


namespace Marketplace.Domain.Users.ValueObjects
{
    public record UserId : ValueObject
    {
        public Guid Value { get; init; }

        private UserId(Guid value) => Value = value;

        public static UserId New() => new(Guid.NewGuid());
        public static UserId From(Guid value) => new(value);

        public static implicit operator Guid(UserId userId) => userId.Value;
        public static implicit operator UserId(Guid value) => From(value);

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Value;
        }
    }
}
