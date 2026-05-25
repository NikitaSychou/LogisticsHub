namespace LogisticsHub.Results;

public sealed record Error
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public Error(
        string code,
        string description,
        IReadOnlyDictionary<string, object?>? metadata = null)
    {
        Code = code;
        Description = description;
        Metadata = metadata ?? new Dictionary<string, object?>();
    }

    public string Code { get; }

    public string Description { get; }

    public IReadOnlyDictionary<string, object?> Metadata { get; }
}
