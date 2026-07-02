using Marketplace.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Marketplace.Infrastructure.Persistence.Seeders;

public class SupportChatSeeder : IDbSeeder
{
    public async Task SeedAsync(ApplicationDbContext context, IServiceProvider sp, CancellationToken ct = default)
    {
        if (await context.Chats.AnyAsync(ct))
            return;

        var rng = Random.Shared;
        var now = DateTime.UtcNow;
        var allBuyers = await context.MarketplaceUsers.Where(u => u.Role == 1).ToListAsync(ct);
        var allSellers = await context.MarketplaceUsers.Where(u => u.Role == 2).ToListAsync(ct);
        var companies = await context.Companies.ToListAsync(ct);
        var orders = await context.Orders.ToListAsync(ct);
        var admin = await context.MarketplaceUsers.FirstAsync(u => u.Role == 4, ct);

        var buyers = allBuyers.OrderBy(_ => rng.Next()).Take(15).ToList();
        var sellers = allSellers.OrderBy(_ => rng.Next()).Take(5).ToList();

        var chatMessages = new[] { "Доброго дня! Ця книга ще в наявності?", "Так, є в наявності.", "Чи можливий самовивіз?", "Так, самовивіз можливий.", "Дякую за швидку відповідь!", "Коли буде доставка?", "Замовлення вже відправлено.", "Чи можна повернути книгу?", "Так, протягом 14 днів." };
        var ticketSubjects = new[] { "Проблема з доставкою", "Повернення товару", "Питання щодо оплати", "Пошкоджена книга" };

        foreach (var buyer in buyers)
        {
            var seller = sellers[rng.Next(sellers.Count)];
            var company = companies[rng.Next(companies.Count)];
            var chatId = Guid.NewGuid();
            var msgCount = rng.Next(2, 5);
            var chatCreated = now.AddDays(-rng.Next(10, 30));

            context.Chats.Add(new ChatRecord
            {
                Id = chatId, Type = 1, Status = 1, InitiatorUserId = buyer.Id,
                OrderId = orders[rng.Next(orders.Count)].Id,
                LastMessageText = chatMessages[rng.Next(chatMessages.Length)],
                LastMessageSenderId = rng.Next(2) == 0 ? buyer.Id : seller.Id,
                LastMessageCreatedAt = now.AddDays(-rng.Next(0, 3)), IsActive = true,
                CreatedAt = chatCreated, UpdatedAt = now,
            });

            context.ChatParticipants.AddRange(
                new ChatParticipantRecord { ChatId = chatId, UserId = buyer.Id, Role = 1, CreatedAt = chatCreated, UpdatedAt = chatCreated },
                new ChatParticipantRecord { ChatId = chatId, UserId = seller.Id, Role = 2, CompanyId = company.Id, CreatedAt = chatCreated, UpdatedAt = chatCreated }
            );

            for (int i = 0; i < msgCount; i++)
            {
                context.ChatMessages.Add(new ChatMessageRecord
                {
                    ChatId = chatId, SenderId = i % 2 == 0 ? buyer.Id : seller.Id,
                    Text = chatMessages[(rng.Next(chatMessages.Length) + i) % chatMessages.Length],
                    Status = 1, ReadAt = i < msgCount - 1 ? chatCreated.AddHours(i + 1) : null,
                    CreatedAt = chatCreated.AddMinutes(i * 15), UpdatedAt = chatCreated.AddMinutes(i * 15),
                });
            }

            context.ChatReadStates.Add(new ChatReadStateRecord { ChatId = chatId, UserId = buyer.Id, LastReadMessageId = msgCount, UpdatedAt = now });
        }

        foreach (var buyer in buyers.Take(8))
        {
            var ticket = new SupportTicketRecord
            {
                TicketNumber = $"TKT-{now:yyyyMMdd}-{rng.Next(1000, 9999)}",
                UserId = buyer.Id.ToString(), Subject = ticketSubjects[rng.Next(ticketSubjects.Length)],
                Message = "Потрібна допомога з моїм замовленням.", Status = 1, Priority = 2,
                CreatedAt = now.AddDays(-rng.Next(5, 20)), UpdatedAt = now,
            };
            context.SupportTickets.Add(ticket);
            await context.SaveChangesAsync(ct);

            context.SupportTicketMessages.AddRange(
                new SupportTicketMessageRecord { TicketId = ticket.Id, SenderId = buyer.Id.ToString(), Message = ticket.Message, CreatedAt = ticket.CreatedAt, UpdatedAt = ticket.CreatedAt },
                new SupportTicketMessageRecord { TicketId = ticket.Id, SenderId = admin.Id.ToString(), Message = "Ми опрацьовуємо ваше звернення. Очікуйте.", IsInternal = false, CreatedAt = ticket.CreatedAt.AddHours(1), UpdatedAt = ticket.CreatedAt.AddHours(1) }
            );
            context.SupportTicketEvents.Add(new SupportTicketEventRecord { TicketId = ticket.Id, EventType = 1, ActorUserId = buyer.Id.ToString(), Reason = "Створено", CreatedAt = ticket.CreatedAt });
            context.SupportTicketAssignments.Add(new SupportTicketAssignmentRecord { TicketId = ticket.Id, AssigneeUserId = admin.Id.ToString(), AssignedByUserId = admin.Id.ToString(), Reason = "Авто", CreatedAt = ticket.CreatedAt.AddMinutes(5) });
        }

        await context.SaveChangesAsync(ct);
    }
}
