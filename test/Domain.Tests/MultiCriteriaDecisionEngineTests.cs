using Domain;

namespace Domain.Tests;

[TestFixture]
public class MultiCriteriaDecisionEngineTests
{
    private readonly MultiCriteriaDecisionEngine _engine = new();

    // ── Cost criterion: 0 (DNF) or missing must not score as the best finish ────

    [Test]
    public void CostCriterion_ZeroValue_ResolvesToWorstNotBest()
    {
        // last6-style: lower finish position is better. "A" DNF'd (0); "B" and "C" have real
        // finishes, "C" (7) being the worst real one. A must resolve to the same normalized
        // score as the worst real performer (0.0), not to the best just because 0 < 4 < 7.
        var criteria = new[] { new Criterion("last6_1", 0, OptimizationType.Minimize), new Criterion("odds", 1, OptimizationType.Maximize) };
        var alternatives = new[]
        {
            new Alternative("A", new Dictionary<string, double?> { ["last6_1"] = 0, ["odds"] = 5 }),
            new Alternative("B", new Dictionary<string, double?> { ["last6_1"] = 4, ["odds"] = 5 }),
            new Alternative("C", new Dictionary<string, double?> { ["last6_1"] = 7, ["odds"] = 5 }),
        };

        var result = _engine.Analyze(criteria, alternatives);

        Assert.That(result.IsSuccess, Is.True);
        var normalizedA = NormalizedValue(result.Value, "A", "last6_1");
        Assert.That(normalizedA, Is.EqualTo(0.0));
    }

    [Test]
    public void CostCriterion_MissingValue_ResolvesSameAsZero()
    {
        var criteria = new[] { new Criterion("last6_1", 0, OptimizationType.Minimize), new Criterion("odds", 1, OptimizationType.Maximize) };
        var alternatives = new[]
        {
            new Alternative("A", new Dictionary<string, double?> { ["last6_1"] = null, ["odds"] = 5 }),
            new Alternative("B", new Dictionary<string, double?> { ["last6_1"] = 4, ["odds"] = 5 }),
            new Alternative("C", new Dictionary<string, double?> { ["last6_1"] = 7, ["odds"] = 5 }),
        };

        var result = _engine.Analyze(criteria, alternatives);

        Assert.That(result.IsSuccess, Is.True);
        var normalizedA = NormalizedValue(result.Value, "A", "last6_1");
        Assert.That(normalizedA, Is.EqualTo(0.0));
    }

    // ── Benefit criterion: missing must not score as best; a real 0 is untouched ─

    [Test]
    public void BenefitCriterion_MissingValue_ResolvesToWorst()
    {
        var criteria = new[] { new Criterion("win_rate", 0, OptimizationType.Maximize), new Criterion("odds", 1, OptimizationType.Maximize) };
        var alternatives = new[]
        {
            new Alternative("A", new Dictionary<string, double?> { ["win_rate"] = null, ["odds"] = 5 }),
            new Alternative("B", new Dictionary<string, double?> { ["win_rate"] = 10, ["odds"] = 5 }),
            new Alternative("C", new Dictionary<string, double?> { ["win_rate"] = 20, ["odds"] = 5 }),
        };

        var result = _engine.Analyze(criteria, alternatives);

        Assert.That(result.IsSuccess, Is.True);
        var normalizedA = NormalizedValue(result.Value, "A", "win_rate");
        Assert.That(normalizedA, Is.EqualTo(0.0));
    }

    [Test]
    public void BenefitCriterion_RealZero_IsNotTreatedAsMissing()
    {
        // A real 0% win rate is already the worst possible value for a Benefit criterion —
        // it must normalize the same as if it were the literal min, not get bumped up.
        var criteria = new[] { new Criterion("win_rate", 0, OptimizationType.Maximize), new Criterion("odds", 1, OptimizationType.Maximize) };
        var alternatives = new[]
        {
            new Alternative("A", new Dictionary<string, double?> { ["win_rate"] = 0, ["odds"] = 5 }),
            new Alternative("B", new Dictionary<string, double?> { ["win_rate"] = 10, ["odds"] = 5 }),
        };

        var result = _engine.Analyze(criteria, alternatives);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(NormalizedValue(result.Value, "A", "win_rate"), Is.EqualTo(0.0));
    }

    // ── All-missing column: neutral tie, run still succeeds ─────────────────────

    [Test]
    public void AllAlternativesMissingForCriterion_DoesNotFailAndTiesNeutrally()
    {
        var criteria = new[] { new Criterion("last6_6", 0, OptimizationType.Minimize), new Criterion("odds", 1, OptimizationType.Maximize) };
        var alternatives = new[]
        {
            new Alternative("A", new Dictionary<string, double?> { ["last6_6"] = null, ["odds"] = 5 }),
            new Alternative("B", new Dictionary<string, double?> { ["last6_6"] = null, ["odds"] = 8 }),
        };

        var result = _engine.Analyze(criteria, alternatives);

        Assert.That(result.IsSuccess, Is.True);
        foreach (var alt in result.Value.Saw.Ranking)
        {
            var contribution = alt.Breakdown.First(b => b.Key == "last6_6");
            Assert.That(contribution.Normalized, Is.EqualTo(0.5));
        }
    }

    private static double NormalizedValue(EngineAnalysis analysis, string alternativeId, string criterionKey) =>
        analysis.Saw.Ranking
            .First(r => r.AlternativeId == alternativeId).Breakdown
            .First(b => b.Key == criterionKey)
            .Normalized;
}