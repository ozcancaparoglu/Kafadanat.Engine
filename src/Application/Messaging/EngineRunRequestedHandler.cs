using System.Text.Json;
using System.Text.Json.Serialization;
using Domain;
using Kafadanat.Contracts;
using Onspay.Cqrs.Inbox;
using Onspay.Infrastructure.Outbox;
using Onspay.SharedKernel;

namespace Application.Messaging;

internal sealed class EngineRunRequestedHandler(
    IMultiCriteriaDecisionEngine engine,
    IOutboxDbContext db)
    : InboxMessageHandlerBase<EngineRunRequested>
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public override string MessageType => nameof(EngineRunRequested);

    public override async Task<Result> HandleAsync(EngineRunRequested message, CancellationToken cancellationToken)
    {
        var (criteria, alternatives) = RunMapper.FromRequest(message);
        var result = engine.Analyze(criteria, alternatives);

        if (result.IsFailure)
        {
            var failed = new EngineRunFailed(message.RunId, result.Error.Code, result.Error.Description);
            db.OutboxMessages.Add(OutboxMessage.Create(
                nameof(EngineRunFailed),
                JsonSerializer.Serialize(failed, JsonOpts),
                RoutingKeys.EngineRunFailed));
        }
        else
        {
            var completed = RunMapper.ToCompleted(message.RunId, result.Value);
            db.OutboxMessages.Add(OutboxMessage.Create(
                nameof(EngineRunCompleted),
                JsonSerializer.Serialize(completed, JsonOpts),
                RoutingKeys.EngineRunCompleted));
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
