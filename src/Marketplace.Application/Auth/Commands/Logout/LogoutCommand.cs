using Marketplace.Domain.Shared.Kernel;
using MediatR;
using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Auth.Commands.Logout
{
    public record LogoutCommand(
      Guid UserId
  ) : IRequest<Result>;
}
