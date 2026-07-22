using Microsoft.EntityFrameworkCore;
using Onspay.Infrastructure.DomainEvents;
using Onspay.Infrastructure.Inbox;
using Onspay.Infrastructure.Outbox;
using Onspay.SharedKernel;

namespace Infrastructure.Database;

public sealed class EngineDbContext(
    DbContextOptions<EngineDbContext> options,
    IDomainEventDispatcher dispatcher,
    IDateTimeProvider dateTimeProvider)
    : DomainEventDbContextBase(options, dispatcher, dateTimeProvider), IInboxDbContext, IOutboxDbContext
{
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.HasDefaultSchema(Schemas.Message);
        mb.ApplyConfiguration(new InboxMessageConfiguration());
        mb.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
