using Marketplace.Application.Auth.DTOs;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Auth.Commands.Login
{
    public record LoginCommand(
      string Email,
      string Password,
      bool RememberMe = false,
      string? TwoFactorCode = null
  ) : IRequest<Result<AuthTokensDto>>;
}
