using Onspay.SharedKernel;

namespace Domain;

public sealed class MultiCriteriaDecisionEngine : IMultiCriteriaDecisionEngine
{
    public Result<EngineAnalysis> Analyze(
        IReadOnlyList<Criterion> criteria,
        IReadOnlyList<Alternative> alternatives)
    {
        var error = Validate(criteria, alternatives);
        if (error is not null)
            return Result.Failure<EngineAnalysis>(error);

        var sorted = criteria.OrderBy(c => c.Rank).ToList();
        var resolved = ResolveEffectiveValues(sorted, alternatives);
        var weights = ComputeRocWeights(sorted);
        var saw = ComputeSaw(sorted, alternatives, resolved, weights);
        var topsis = ComputeTopsis(sorted, alternatives, resolved, weights);
        var comparison = ComputeComparison(alternatives, saw, topsis);

        var criterionWeights = sorted
            .Select(c => new CriterionWeightResult(c.Key, c.Rank, c.OptimizationType, weights[c.Key]))
            .ToList();

        return Result.Success(new EngineAnalysis(criterionWeights, saw, topsis, comparison));
    }

    private static Error? Validate(IReadOnlyList<Criterion> criteria, IReadOnlyList<Alternative> alternatives)
    {
        if (criteria.Count < 2) return EngineErrors.TooFewCriteria;
        if (alternatives.Count < 2) return EngineErrors.TooFewAlternatives;
        if (criteria.Select(c => c.Key).Distinct().Count() != criteria.Count) return EngineErrors.DuplicateCriterionKeys;
        if (criteria.Select(c => c.Rank).Distinct().Count() != criteria.Count) return EngineErrors.DuplicateCriterionRanks;

        foreach (var alt in alternatives)
        {
            foreach (var c in criteria)
            {
                if (!alt.Values.TryGetValue(c.Key, out var val))
                    return EngineErrors.MissingCriterionValue(alt.Id, c.Key);
                if (val is not null && !double.IsFinite(val.Value))
                    return EngineErrors.InvalidValue;
            }
        }

        return null;
    }

    // Missing/DNF values are resolved relative to the other alternatives in this same run,
    // based on the criterion's direction: a Cost value of 0 (impossible for e.g. a real finish
    // position — it always signals "did not finish") or an absent value is treated as no better
    // than the worst real performer (the column's max); a Benefit value that's absent (0 is a
    // legitimate real worst value there, so it's left alone) is treated the same way via the
    // column's min. If a column has no real values at all, everyone gets the same placeholder so
    // the existing max-min-epsilon tie-break in ComputeSaw scores it as a neutral 0.5 for all.
    private static Dictionary<string, Dictionary<string, double>> ResolveEffectiveValues(
        IReadOnlyList<Criterion> sorted,
        IReadOnlyList<Alternative> alternatives)
    {
        var resolved = alternatives.ToDictionary(a => a.Id, _ => new Dictionary<string, double>(sorted.Count));

        foreach (var c in sorted)
        {
            var real = new List<double>(alternatives.Count);
            foreach (var alt in alternatives)
            {
                var v = alt.Values[c.Key];
                if (!IsMissing(v, c.OptimizationType))
                    real.Add(v!.Value);
            }

            double placeholder = real.Count > 0
                ? (c.OptimizationType == OptimizationType.Minimize ? real.Max() : real.Min())
                : 0.0;

            foreach (var alt in alternatives)
            {
                var v = alt.Values[c.Key];
                resolved[alt.Id][c.Key] = IsMissing(v, c.OptimizationType) ? placeholder : v!.Value;
            }
        }

        return resolved;
    }

    private static bool IsMissing(double? value, OptimizationType type) =>
        value is null || (type == OptimizationType.Minimize && value == 0);

    // ROC weight formula: w_i = (1/n) * Σ(1/j) for j = i..n (rank 1 is heaviest)
    private static Dictionary<string, double> ComputeRocWeights(IReadOnlyList<Criterion> sorted)
    {
        int n = sorted.Count;
        var weights = new Dictionary<string, double>(n);
        for (int i = 0; i < n; i++)
        {
            double w = 0;
            for (int j = i + 1; j <= n; j++)
                w += 1.0 / j;
            weights[sorted[i].Key] = w / n;
        }
        return weights;
    }

    private static SawEngineResult ComputeSaw(
        IReadOnlyList<Criterion> sorted,
        IReadOnlyList<Alternative> alternatives,
        Dictionary<string, Dictionary<string, double>> resolved,
        Dictionary<string, double> weights)
    {
        var minMax = new Dictionary<string, (double min, double max)>(sorted.Count);
        foreach (var c in sorted)
        {
            var vals = alternatives.Select(a => resolved[a.Id][c.Key]);
            minMax[c.Key] = (vals.Min(), vals.Max());
        }

        var scores = new List<(string id, double score, List<(string key, double norm, double weighted)> details)>(alternatives.Count);
        foreach (var alt in alternatives)
        {
            double total = 0;
            var details = new List<(string key, double norm, double weighted)>(sorted.Count);
            foreach (var c in sorted)
            {
                var (min, max) = minMax[c.Key];
                double raw = resolved[alt.Id][c.Key];
                double norm = max - min < double.Epsilon
                    ? 0.5
                    : c.OptimizationType == OptimizationType.Maximize
                        ? (raw - min) / (max - min)
                        : (max - raw) / (max - min);
                double wv = norm * weights[c.Key];
                total += wv;
                details.Add((c.Key, norm, wv));
            }
            scores.Add((alt.Id, total, details));
        }

        var ranked = scores
            .OrderByDescending(s => s.score)
            .ThenBy(s => s.id, StringComparer.Ordinal)
            .ToList();

        double maxScore = ranked.Count > 0 ? ranked[0].score : 1.0;

        var ranking = ranked.Select((s, i) =>
        {
            double pct = maxScore > double.Epsilon ? s.score / maxScore * 100.0 : 0.0;
            var breakdown = s.details.Select(d =>
            {
                double share = s.score > double.Epsilon ? d.weighted / s.score * 100.0 : 0.0;
                return new CriterionContributionScore(d.key, d.norm, weights[d.key], d.weighted, share);
            }).ToList();
            return new SawAlternativeScore(s.id, i + 1, s.score, pct, breakdown);
        }).ToList();

        return new SawEngineResult(ranking);
    }

    private static TopsisEngineResult ComputeTopsis(
        IReadOnlyList<Criterion> sorted,
        IReadOnlyList<Alternative> alternatives,
        Dictionary<string, Dictionary<string, double>> resolved,
        Dictionary<string, double> weights)
    {
        // Step 1: vector normalization
        var norms = new Dictionary<string, double>(sorted.Count);
        foreach (var c in sorted)
        {
            double sumSq = alternatives.Sum(a => resolved[a.Id][c.Key] * resolved[a.Id][c.Key]);
            norms[c.Key] = Math.Sqrt(sumSq);
        }

        // Step 2: weighted normalized matrix
        var wm = new Dictionary<string, Dictionary<string, double>>(alternatives.Count);
        foreach (var alt in alternatives)
        {
            var row = new Dictionary<string, double>(sorted.Count);
            foreach (var c in sorted)
            {
                double r = norms[c.Key] > 0 ? resolved[alt.Id][c.Key] / norms[c.Key] : 0.0;
                row[c.Key] = r * weights[c.Key];
            }
            wm[alt.Id] = row;
        }

        // Step 3: ideal and negative-ideal
        var ideal = new Dictionary<string, double>(sorted.Count);
        var negIdeal = new Dictionary<string, double>(sorted.Count);
        foreach (var c in sorted)
        {
            var vals = alternatives.Select(a => wm[a.Id][c.Key]).ToList();
            if (c.OptimizationType == OptimizationType.Maximize)
            {
                ideal[c.Key] = vals.Max();
                negIdeal[c.Key] = vals.Min();
            }
            else
            {
                ideal[c.Key] = vals.Min();
                negIdeal[c.Key] = vals.Max();
            }
        }

        // Steps 4 & 5: distances and closeness
        var scores = new List<(string id, double sPlus, double sMinus, double closeness)>(alternatives.Count);
        foreach (var alt in alternatives)
        {
            double dPlus = 0, dMinus = 0;
            foreach (var c in sorted)
            {
                double v = wm[alt.Id][c.Key];
                double dp = v - ideal[c.Key];
                double dn = v - negIdeal[c.Key];
                dPlus += dp * dp;
                dMinus += dn * dn;
            }
            double sPlus = Math.Sqrt(dPlus);
            double sMinus = Math.Sqrt(dMinus);
            double denom = sPlus + sMinus;
            double closeness = denom > double.Epsilon ? sMinus / denom : 0.0;
            scores.Add((alt.Id, sPlus, sMinus, closeness));
        }

        var ranked = scores
            .OrderByDescending(s => s.closeness)
            .ThenBy(s => s.id, StringComparer.Ordinal)
            .ToList();

        var ranking = ranked
            .Select((s, i) => new TopsisAlternativeScore(s.id, i + 1, s.sPlus, s.sMinus, s.closeness, s.closeness * 100.0))
            .ToList();

        return new TopsisEngineResult(ideal, negIdeal, ranking);
    }

    private static ComparisonEngineResult ComputeComparison(
        IReadOnlyList<Alternative> alternatives,
        SawEngineResult saw,
        TopsisEngineResult topsis)
    {
        var sawRanks = saw.Ranking.ToDictionary(r => r.AlternativeId, r => r.Position);
        var topsisRanks = topsis.Ranking.ToDictionary(r => r.AlternativeId, r => r.Position);

        int n = alternatives.Count;
        int exact = 0, withinOne = 0;
        var perAlt = new List<AlternativeRankComparison>(n);

        foreach (var alt in alternatives.OrderBy(a => sawRanks[a.Id]))
        {
            int sr = sawRanks[alt.Id];
            int tr = topsisRanks[alt.Id];
            int diff = Math.Abs(sr - tr);
            if (diff == 0) exact++;
            if (diff <= 1) withinOne++;
            perAlt.Add(new AlternativeRankComparison(alt.Id, sr, tr, diff));
        }

        return new ComparisonEngineResult(
            n > 0 ? (double)exact / n * 100.0 : 0.0,
            n > 0 ? (double)withinOne / n * 100.0 : 0.0,
            perAlt);
    }
}
