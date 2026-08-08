namespace BaseWebApi.Shared.Extensions;

public static class CollectionExtensions
{
    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? source) =>
        source is null || !source.Any();

    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> source)
        where T : class =>
        source.Where(x => x is not null)!;

    public static IReadOnlyList<T> ToReadOnlyList<T>(this IEnumerable<T> source) =>
        source as IReadOnlyList<T> ?? source.ToList();

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public static IEnumerable<IReadOnlyList<T>> Batch<T>(this IEnumerable<T> source, int size)
    {
        if (size <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size));
        }

        var batch = new List<T>(size);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count != size)
            {
                continue;
            }

            yield return batch;
            batch = new List<T>(size);
        }

        if (batch.Count > 0)
        {
            yield return batch;
        }
    }
}
