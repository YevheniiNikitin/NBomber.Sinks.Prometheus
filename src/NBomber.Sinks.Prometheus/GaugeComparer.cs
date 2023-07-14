namespace NBomber.Sinks.Prometheus;

// TODO: remove it when a synchronous gauge will be implemented
/// <summary>
/// This Comparer ensures that we will create a new Gauge for each set of tags even if the name of the gauge is the same.
/// </summary>
public sealed class GaugeComparer : IEqualityComparer<(string name, KeyValuePair<string, object?>[] tags)>
{
    public bool Equals(
        (string name, KeyValuePair<string, object?>[] tags) x,
        (string name, KeyValuePair<string, object?>[] tags) y) =>
        x.name == y.name;

    public int GetHashCode((string name, KeyValuePair<string, object?>[] tags) key) =>
        key.name.GetHashCode() ^ GetTagsHash(key.tags);

    private static int GetTagsHash(KeyValuePair<string, object?>[] tags)
    {
        int hash = 17; // prime number

        var span = tags.AsSpan();
        for (int i = 0; i < span.Length; i++)
        {
            var item = span[i];
            var combinedHash = CombineHashCodes(item.Key.GetHashCode(), item.Value?.GetHashCode() ?? default);
            hash = (hash * 31) + combinedHash;
        }

        return hash;
    }

    private static int CombineHashCodes(int h1, int h2)
    {
        unchecked
        {
            return ((h1 << 5) + h1) ^ h2;
        }
    }
}