using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Domain.Common.Exceptions
{
    public class BusinessRuleValidationException : DomainException
    {
        public string Rule { get; }

        public BusinessRuleValidationException(string rule, string message)
            : base(message)
        {
            Rule = rule;
        }
    }
}
