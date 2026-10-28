using System.Text.Json;

namespace ChainedBeasts.DivineNumbers;

internal sealed record NumericDetail(string Kind, int Id, int Severity, string Name, string Body);

internal sealed class DetailCatalog
{
    private readonly Dictionary<(string Kind, int Id, int Severity), NumericDetail> _entries = new();
    private readonly Dictionary<string, NumericDetail> _terms = new(StringComparer.Ordinal);
    internal DetailCatalog(Stream source)
    {
        using var json = JsonDocument.Parse(source);
        foreach (var row in json.RootElement.GetProperty("entries").EnumerateArray())
        {
            var kind = row.GetProperty("kind").GetString()!;
            var id = row.GetProperty("id").GetInt32();
            var severity = row.GetProperty("severity").GetInt32();
            var name = row.GetProperty("name").GetString()!;
            var lines = row.GetProperty("details").EnumerateArray().Select(x => x.GetString()!).ToArray();
            if (kind is not ("skill" or "injury" or "purchase" or "status") ||
                (kind == "injury" ? severity is < 1 or > 3 : severity != 0) ||
                string.IsNullOrWhiteSpace(name) || lines.Length == 0 || lines.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("Invalid numeric detail entry: " + kind + "/" + id);
            var detail = new NumericDetail(kind, id, severity, name, string.Join("\n", lines));
            _entries.Add((kind, id, severity), detail);
            foreach (var term in row.GetProperty("terms").EnumerateArray())
            {
                var key = term.GetString();
                if (string.IsNullOrWhiteSpace(key)) throw new InvalidDataException("Empty description key");
                _terms.Add(key!, detail);
            }
        }
    }
    internal int Count => _entries.Count;
    internal bool TryGet(int skill, out NumericDetail detail) => TryGet("skill", skill, 0, out detail);
    internal bool TryGet(string kind, int id, int severity, out NumericDetail detail) =>
        _entries.TryGetValue((kind, id, severity), out detail!);
    internal bool TryGetTerm(string? term, out NumericDetail detail)
    {
        detail = null!;
        return term != null && _terms.TryGetValue(term, out detail!);
    }
}
