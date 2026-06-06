namespace Domain;

public sealed record EngineAnalysis(
    IReadOnlyList<CriterionWeightResult> Weights,
    SawEngineResult Saw,
    TopsisEngineResult Topsis,
    ComparisonEngineResult Comparison);

public sealed record CriterionWeightResult(
    string Key,
    int Rank,
    OptimizationType OptimizationType,
    double Weight);

public sealed record SawEngineResult(IReadOnlyList<SawAlternativeScore> Ranking);

public sealed record SawAlternativeScore(
    string AlternativeId,
    int Position,
    double Score,
    double Percent,
    IReadOnlyList<CriterionContributionScore> Breakdown);

public sealed record CriterionContributionScore(
    string Key,
    double Normalized,
    double Weight,
    double Weighted,
    double SharePercent);

public sealed record TopsisEngineResult(
    IReadOnlyDictionary<string, double> IdealPositive,
    IReadOnlyDictionary<string, double> IdealNegative,
    IReadOnlyList<TopsisAlternativeScore> Ranking);

public sealed record TopsisAlternativeScore(
    string AlternativeId,
    int Position,
    double SPlus,
    double SMinus,
    double Closeness,
    double Percent);

public sealed record ComparisonEngineResult(
    double ExactAgreementPercent,
    double WithinOneAgreementPercent,
    IReadOnlyList<AlternativeRankComparison> PerAlternative);

public sealed record AlternativeRankComparison(
    string AlternativeId,
    int SawRank,
    int TopsisRank,
    int Diff);
