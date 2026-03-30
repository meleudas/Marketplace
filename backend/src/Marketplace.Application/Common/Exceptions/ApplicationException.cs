using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Common.Exceptions
{
    public abstract class ApplicationException : Exception
    {
        protected ApplicationException(string message) : base(message) { }

        protected ApplicationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
