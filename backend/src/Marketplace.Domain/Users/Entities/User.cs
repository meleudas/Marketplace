using Marketplace.Domain.Common.Exceptions;
using Marketplace.Domain.Common.Models;
using Marketplace.Domain.Users.Enums;
using Marketplace.Domain.Users.Events;
using Marketplace.Domain.Users.ValueObjects;

namespace Marketplace.Domain.Users.Entities
{
    public class User : AggregateRoot<UserId>
    {
        private User() { }

        public string FirstName { get; private set; } = string.Empty;
        public string LastName { get; private set; } = string.Empty;
        public UserRole Role { get; private set; }
        public DateTime? Birthday { get; private set; }
        public string? Avatar { get; private set; }
        public bool IsVerified { get; private set; }
        public string? VerificationDocument { get; private set; }
        public DateTime? LastLoginAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }
        public bool IsDeleted { get; private set; }
        public DateTime? DeletedAt { get; private set; }

        public static User Create(
            IdentityUserId identityId,
            string firstName,
            string lastName,
            UserRole role = UserRole.Buyer,
            DateTime? birthday = null,
            string? avatar = null)
        {
            ValidateName(firstName, nameof(firstName));
            ValidateName(lastName, nameof(lastName));

            var now = DateTime.UtcNow;
            var user = new User
            {
                Id = UserId.From(identityId.Value),
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Role = role,
                Birthday = birthday,
                Avatar = avatar,
                IsVerified = false,
                VerificationDocument = null,
                LastLoginAt = null,
                CreatedAt = now,
                UpdatedAt = now,
                IsDeleted = false,
                DeletedAt = null
            };

            user.AddDomainEvent(new UserRegisteredEvent(user.Id));
            return user;
        }

        public static User Reconstitute(
            UserId id,
            string firstName,
            string lastName,
            UserRole role,
            DateTime? birthday,
            string? avatar,
            bool isVerified,
            string? verificationDocument,
            DateTime? lastLoginAt,
            DateTime createdAt,
            DateTime updatedAt,
            bool isDeleted,
            DateTime? deletedAt) =>
            new()
            {
                Id = id,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                Birthday = birthday,
                Avatar = avatar,
                IsVerified = isVerified,
                VerificationDocument = verificationDocument,
                LastLoginAt = lastLoginAt,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt,
                IsDeleted = isDeleted,
                DeletedAt = deletedAt
            };

        public void Verify(string? verificationDocument = null)
        {
            if (IsDeleted)
                throw new DomainException("Cannot verify deleted user");

            IsVerified = true;
            VerificationDocument = verificationDocument;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateProfile(
            string firstName,
            string lastName,
            DateTime? birthday,
            string? avatar)
        {
            if (IsDeleted)
                throw new DomainException("Cannot update deleted user");

            ValidateName(firstName, nameof(firstName));
            ValidateName(lastName, nameof(lastName));

            FirstName = firstName.Trim();
            LastName = lastName.Trim();
            Birthday = birthday;
            Avatar = avatar;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SetRole(UserRole role)
        {
            if (IsDeleted)
                throw new DomainException("Cannot change role for deleted user");

            Role = role;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete()
        {
            if (IsDeleted)
                return;

            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            AddDomainEvent(new UserDeletedEvent(Id));
        }

        private static void ValidateName(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException($"{fieldName} cannot be empty");

            if (value.Trim().Length > 100)
                throw new DomainException($"{fieldName} length must be <= 100");
        }
    }
}
