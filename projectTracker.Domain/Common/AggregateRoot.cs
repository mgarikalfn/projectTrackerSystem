using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace projectTracker.Domain.Common
{
    public abstract class AggregateRoot<TId> where TId : notnull
    {
        public TId Id { get; protected set; }

        // For EF Core
        protected AggregateRoot() { }

        protected AggregateRoot(TId id)
        {
            Id = id;
        }

       

        // Equality comparison
        public override bool Equals(object? obj)
        {
            if (obj is not AggregateRoot<TId> other)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return Id.Equals(other.Id);
        }

        public override int GetHashCode() => Id.GetHashCode();
    }
}
