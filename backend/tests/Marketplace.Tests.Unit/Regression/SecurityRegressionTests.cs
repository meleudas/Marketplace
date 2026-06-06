using System.Security.Claims;
using Marketplace.API.Controllers;
using Marketplace.API.Options;
using Marketplace.Application.Common.Ports;
using Marketplace.Application.Favorites.DTOs;
using Marketplace.Application.Orders.DTOs;
using Marketplace.Application.Notifications.Commands.MarkNotificationRead;
using Marketplace.Application.Notifications.Ports;
using Marketplace.Application.Notifications.Queries.GetMyNotifications;
using Marketplace.Application.Orders.Commands.CancelOrder;
using Marketplace.Domain.Users.ValueObjects;
using Marketplace.Infrastructure.Identity;
using Marketplace.Infrastructure.Identity.Services;
using Marketplace.Domain.Shared.Kernel;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Marketplace.Tests;

public sealed class SecurityRegressionTests
{
    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Orders")]
    public async Task Orders_ListMy_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new OrdersController(new RecordingSender(), new StartedIdempotencyStore())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.ListMy(null, null, null, null, null, 1, 20, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Orders")]
    public async Task Cancel_Returns_BadRequest_When_Idempotency_Missing()
    {
        var sender = new RecordingSender { NextResult = Result.Success() };
        var controller = BuildAuthorizedOrdersController(sender);

        var result = await controller.Cancel(7, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Orders")]
    public async Task Orders_ListCompany_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new OrdersController(new RecordingSender
        {
            NextResult = Result<PagedOrdersDto>.Success(new PagedOrdersDto([], 0, 1, 20))
        }, new StartedIdempotencyStore())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.ListCompany(Guid.NewGuid(), null, null, null, null, null, 1, 20, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Orders")]
    public async Task Orders_GetCompany_Returns_Forbidden_For_CrossTenant_Order()
    {
        var controller = BuildAuthorizedOrdersController(new RecordingSender
        {
            NextResult = Result<OrderDetailsDto>.Failure("Forbidden")
        });

        var result = await controller.GetCompany(Guid.NewGuid(), 101, CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, obj.StatusCode);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Payments")]
    public async Task Webhook_Returns_Unauthorized_On_Failed_Command_Result()
    {
        var sender = new RecordingSender { NextResult = Result.Failure("Invalid LiqPay signature") };
        var controller = new PaymentsIntegrationsController(sender, new StartedIdempotencyStore())
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };

        var result = await controller.Webhook(new LiqPayWebhookRequest("bad", "bad"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Payments")]
    public async Task AdminPayments_Refund_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new AdminPaymentsController(new RecordingSender { NextResult = Result.Success() })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Refund(11, new RequestRefundBody(20m, "test"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Payments")]
    public async Task AdminPayments_Sync_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new AdminPaymentsController(new RecordingSender { NextResult = Result.Success() })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Sync(11, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Platform")]
    public async Task AdminOutbox_Requeue_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new AdminOutboxController(new SpyOutboxWriter())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Requeue(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Platform")]
    public async Task AdminOutbox_Requeue_Returns_Forbid_For_NonAdmin()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Buyer")
        ], "test");
        var controller = new AdminOutboxController(new SpyOutboxWriter())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };

        var result = await controller.Requeue(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "ProductsModeration")]
    public async Task AdminProducts_Approve_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new AdminProductsController(new RecordingSender { NextResult = Result.Success() })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Approve(55, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "ProductsModeration")]
    public async Task Products_Create_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new ProductsController(new RecordingSender { NextResult = Result.Failure("Forbidden") }, Options.Create(new Marketplace.Application.Common.Options.StorageOptions()))
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Create(
            Guid.NewGuid(),
            new UpsertProductRequest("N", "n", "d", 10, null, 0, 1, false, null, null),
            CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Favorites")]
    public async Task Favorites_Get_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new FavoritesController(new RecordingSender
        {
            NextResult = Result<IReadOnlyList<FavoriteItemDto>>.Success([])
        }, NullLogger<FavoritesController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.GetMyFavorites(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Favorites")]
    public async Task Favorites_Add_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new FavoritesController(new RecordingSender
        {
            NextResult = Result<FavoriteItemDto>.Success(new FavoriteItemDto(1, 1, DateTime.UtcNow, 10m, true))
        }, NullLogger<FavoritesController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Add(10, CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "IdentityAccess")]
    public async Task Auth_Logout_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new AuthController(
            new RecordingSender { NextResult = Result.Success() },
            Options.Create(new CookieAuthOptions
            {
                RefreshTokenCookieName = "refresh_token",
                RefreshTokenDays = 30
            }),
            NullLogger<AuthController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Logout(CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "IdentityAccess")]
    public void TokenService_Rejects_Expired_Jwt()
    {
        var options = Options.Create(new JwtOptions
        {
            SecretKey = "ExpiredTokenTestKey_AtLeast32Bytes!!",
            Issuer = "Marketplace.Tests",
            Audience = "Marketplace.Tests",
            AccessTokenMinutes = -1,
            RefreshTokenDays = 30
        });

        var tokenService = new TokenService(options, userManager: null!);
        var token = tokenService.GenerateAccessToken(IdentityUserId.New(), "user@test.local", ["Admin"]);

        var validation = tokenService.ValidateToken(token.Value);

        Assert.Null(validation);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Notifications")]
    public async Task MeNotifications_List_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new MeNotificationsController(new RecordingSender
        {
            NextResult = Result<PagedInAppNotificationsDto>.Success(
                new PagedInAppNotificationsDto([], 0, 1, 20))
        })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.List(ct: CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Notifications")]
    public async Task MeNotifications_MarkRead_Returns_NotFound_For_Alien_Notification()
    {
        var controller = new MeNotificationsController(new RecordingSender
        {
            NextResult = Result.Failure("Notification not found.")
        })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("sub", Guid.NewGuid().ToString()),
                        new Claim(ClaimTypes.Role, "Buyer")
                    ], "test"))
                }
            }
        };

        var result = await controller.MarkRead(777, CancellationToken.None);

        var obj = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status404NotFound, obj.StatusCode);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Reviews")]
    public async Task ProductReviews_Create_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new ProductReviewsController(new RecordingSender
        {
            NextResult = Result<Marketplace.Application.Reviews.DTOs.ReviewDto>.Failure("Forbidden")
        })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.Create(10, new UpsertProductReviewRequest(5, "t", "c"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Reviews")]
    public async Task AdminReviews_ModerateProduct_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new AdminReviewsController(new RecordingSender { NextResult = Result.Success() })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.ModerateProduct(10, new ModerateProductReviewRequest(Marketplace.Domain.Reviews.Enums.ReviewModerationStatus.Hidden), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    [Trait("Suite", "Security")]
    [Trait("Suite", "Reviews")]
    public async Task ReviewReplies_UpsertProductReply_Returns_Unauthorized_When_No_Sub_Claim()
    {
        var controller = new ReviewRepliesController(new RecordingSender
        {
            NextResult = Result<Marketplace.Application.Reviews.DTOs.ReviewReplyDto>.Failure("Forbidden")
        })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity()) }
            }
        };

        var result = await controller.UpsertProductReply(10, new UpsertReviewReplyRequest("body"), CancellationToken.None);

        Assert.IsType<UnauthorizedResult>(result);
    }

    private static OrdersController BuildAuthorizedOrdersController(ISender sender)
    {
        var identity = new ClaimsIdentity(
        [
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Admin")
        ], "test");
        return new OrdersController(sender, new StartedIdempotencyStore())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            }
        };
    }

    private sealed class StartedIdempotencyStore : IHttpIdempotencyStore
    {
        public Task<HttpIdempotencyBeginResult> TryBeginAsync(string scope, string idempotencyKey, string requestHash, TimeSpan ttl, CancellationToken ct = default)
            => Task.FromResult(new HttpIdempotencyBeginResult(HttpIdempotencyBeginState.Started, null));

        public Task CompleteAsync(string scope, string idempotencyKey, string requestHash, int statusCode, string? responseBodyJson, CancellationToken ct = default)
            => Task.CompletedTask;
    }

    private sealed class RecordingSender : ISender
    {
        public object? NextResult { get; set; } = Result.Success();

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult((TResponse)NextResult!);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult(NextResult);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => Empty<object?>();

        private static async IAsyncEnumerable<T> Empty<T>()
        {
            await Task.CompletedTask;
            yield break;
        }
    }

    private sealed class SpyOutboxWriter : IOutboxWriter
    {
        public Task AppendAsync(string aggregateType, string aggregateId, string eventType, string payload, CancellationToken ct = default) => Task.CompletedTask;
        public Task<IReadOnlyList<OutboxMessage>> ListPendingAsync(int batchSize, DateTime utcNow, CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<OutboxMessage>>([]);
        public Task MarkProcessedAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkFailedAsync(Guid id, string error, DateTime nextAttemptAtUtc, CancellationToken ct = default) => Task.CompletedTask;
        public Task MarkDeadLetterAsync(Guid id, string reason, string category, CancellationToken ct = default) => Task.CompletedTask;
        public Task RequeueDeadLetterAsync(Guid id, CancellationToken ct = default) => Task.CompletedTask;
    }
}
