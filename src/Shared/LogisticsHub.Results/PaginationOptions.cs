namespace LogisticsHub.Results;

public sealed class PaginationOptions
{
    public int DefaultPageSize { get; set; } = 50;

    public int MaxPageSize { get; set; } = 100;

    public int GetEffectivePageSize()
    {
        if (DefaultPageSize <= 0)
        {
            return 50;
        }

        if (MaxPageSize <= 0)
        {
            return DefaultPageSize;
        }

        return Math.Min(DefaultPageSize, MaxPageSize);
    }
}
