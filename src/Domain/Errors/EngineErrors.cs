using Onspay.SharedKernel;

namespace Domain;

public static class EngineErrors
{
    public static readonly Error TooFewCriteria = new(
        "Engine.TooFewCriteria",
        "At least 2 criteria are required.",
        ErrorType.Validation);

    public static readonly Error TooFewAlternatives = new(
        "Engine.TooFewAlternatives",
        "At least 2 alternatives are required.",
        ErrorType.Validation);

    public static readonly Error DuplicateCriterionKeys = new(
        "Engine.DuplicateCriterionKeys",
        "Criterion keys must be unique.",
        ErrorType.Validation);

    public static readonly Error DuplicateCriterionRanks = new(
        "Engine.DuplicateCriterionRanks",
        "Criterion ranks must be unique.",
        ErrorType.Validation);

    public static Error MissingCriterionValue(string alternativeId, string key) =>
        new("Engine.MissingValue",
            $"Alternative '{alternativeId}' is missing value for criterion '{key}'.",
            ErrorType.Validation);

    public static readonly Error InvalidValue = new(
        "Engine.InvalidValue",
        "Criterion values must be finite numbers.",
        ErrorType.Validation);
}
