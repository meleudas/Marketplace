using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Auth.Ports
{
    public interface IEmailPort
    {
        Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default);
        Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct = default);
        Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct = default);
        Task SendTwoFactorCodeEmailAsync(string to, string code, CancellationToken ct = default);
    }
}
