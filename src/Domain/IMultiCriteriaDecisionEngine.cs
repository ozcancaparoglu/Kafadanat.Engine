using Onspay.SharedKernel;

namespace Domain;

public interface IMultiCriteriaDecisionEngine
{
    Result<EngineAnalysis> Analyze(
        IReadOnlyList<Criterion> criteria,
        IReadOnlyList<Alternative> alternatives);
}
