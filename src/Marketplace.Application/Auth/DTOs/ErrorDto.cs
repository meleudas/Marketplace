using System;
using System.Collections.Generic;
using System.Text;

namespace Marketplace.Application.Auth.DTOs
{
    public record ErrorDto(
      string Code,
      string Message,
      Dictionary<string, string[]>? Details = null
  );
}
