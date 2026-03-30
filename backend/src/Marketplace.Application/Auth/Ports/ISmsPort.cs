using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Auth.Ports
{
    public interface ISmsPort
    {
        Task SendSmsAsync(string phoneNumber, string message, CancellationToken ct = default);
        Task SendVerificationCodeAsync(string phoneNumber, string code, CancellationToken ct = default);
    }
}
