using System.Text.Json;

namespace ChainedBeasts.DivineNumbers;

internal sealed record NumericDetail(int Skill, string Name, string Body);

internal sealed class DetailCatalog
{
    private readonly Dictionary<int, NumericDetail> _skills = new();
    private readonly Dictionary<string, NumericDetail> _terms = new(StringComparer.Ordinal);
    internal DetailCatalog(Stream source)
    {
        using var json = JsonDocument.Parse(source);
        foreach (var row in json.RootElement.GetProperty("abilities").EnumerateArray())
        {
            var skill = row.GetProperty("skillType").GetInt32();
            var name = row.GetProperty("name").GetString()!;
            var lines = row.GetProperty("details").EnumerateArray().Select(x => x.GetString()!).ToArray();
            if (string.IsNullOrWhiteSpace(name) || lines.Length == 0 || lines.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("Empty numeric detail entry: " + skill);
            var detail = new NumericDetail(skill, name, string.Join("\n", lines));
            _skills.Add(skill, detail);
            _terms.Add(row.GetProperty("term").GetString()!, detail);
        }
    }
    internal bool TryGet(int skill, out NumericDetail detail) => _skills.TryGetValue(skill, out detail!);
    internal bool TryGetTerm(string? term, out NumericDetail detail)
    {
        detail = null!;
        return term != null && _terms.TryGetValue(term, out detail!);
    }
}
