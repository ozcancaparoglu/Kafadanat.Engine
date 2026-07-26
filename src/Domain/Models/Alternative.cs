namespace Domain;

public sealed record Alternative(string Id, IReadOnlyDictionary<string, double?> Values);
