using Domain;
using Kafadanat.Contracts;

namespace Application.Messaging;

internal static class RunMapper
{
    public static (IReadOnlyList<Criterion> criteria, IReadOnlyList<Alternative> alternatives)
        FromRequest(EngineRunRequested msg)
    {
        var criteriaList = msg.Criteria.ToList();

        var criteria = criteriaList
            .Select(c => new Criterion(
                c.Key,
                c.Rank,
                c.Direction == "Benefit" ? OptimizationType.Maximize : OptimizationType.Minimize))
            .ToList();

        var alternatives = msg.Alternatives
            .Select((id, i) =>
            {
                var values = new Dictionary<string, double?>(criteriaList.Count);
                for (int j = 0; j < criteriaList.Count; j++)
                    values[criteriaList[j].Key] = (double?)msg.Matrix[i][j];
                return new Alternative(id, values);
            })
            .ToList();

        return (criteria, alternatives);
    }

    public static EngineRunCompleted ToCompleted(Guid runId, EngineAnalysis analysis)
    {
        var weights = analysis.Weights
            .Select(w => new CriterionWeight(
                w.Key, w.Rank,
                w.OptimizationType == OptimizationType.Maximize ? "Benefit" : "Cost",
                w.Weight))
            .ToList();

        var saw = new SawAlgorithmResult(
            "Saw",
            analysis.Saw.Ranking.Select(r => new SawRankedAlternative(
                r.AlternativeId, r.Position, r.Score, r.Percent,
                r.Breakdown.Select(b => new CriterionContribution(
                    b.Key, b.Normalized, b.Weight, b.Weighted, b.SharePercent)).ToList()
            )).ToList());

        var topsis = new TopsisAlgorithmResult(
            "Topsis",
            analysis.Topsis.IdealPositive,
            analysis.Topsis.IdealNegative,
            analysis.Topsis.Ranking.Select(r => new TopsisRankedAlternative(
                r.AlternativeId, r.Position, r.SPlus, r.SMinus, r.Closeness, r.Percent)).ToList());

        var comparison = new RankingComparison(
            analysis.Comparison.ExactAgreementPercent,
            analysis.Comparison.WithinOneAgreementPercent,
            analysis.Comparison.PerAlternative.Select(a => new AlternativeComparison(
                a.AlternativeId, a.SawRank, a.TopsisRank, a.Diff)).ToList());

        return new EngineRunCompleted(runId, weights, saw, topsis, comparison);
    }
}
