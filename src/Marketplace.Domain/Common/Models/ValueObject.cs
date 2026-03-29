using System;
using System.Collections.Generic;
using System.Linq;


namespace Marketplace.Domain.Common.Models
{
    public abstract record ValueObject
    {
        protected abstract IEnumerable<object> GetEqualityComponents();

        public virtual bool Equals(ValueObject? other)
        {
            if (other is null)
                return false;

            if (GetType() != other.GetType())
                return false;

            return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents()
                .Select(x => x?.GetHashCode() ?? 0)
                .Aggregate((x, y) => x ^ y);
        }
    }
}
