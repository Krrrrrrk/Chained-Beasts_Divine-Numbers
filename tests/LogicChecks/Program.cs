using ChainedBeasts.DivineNumbers;

static void Check(bool value, string message)
{ if (!value) throw new InvalidOperationException(message); }

using var input = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "details.json"));
var catalog = new DetailCatalog(input);
Check(catalog.TryGet(0, out var lion) && lion.Body.Contains("+40%") && lion.Body.Contains("10 seconds"), "Lion card missing researched numbers");
Check(catalog.TryGet(3, out var eagle) && eagle.Body.Contains("8 seconds") && eagle.Body.Contains("Consumed"), "Eagle card loses the ready-window/consumption distinction");
Check(catalog.TryGet(7, out var bear) && bear.Body.Contains("minimum 0.3") && bear.Body.Contains("preserved"), "Bear card treats minimum stun as extra stun");
Check(catalog.Count >= 190, "Expanded catalog was replaced with the three-card sample");
foreach (var id in new[] { -1, 11010, int.MaxValue })
    Check(!catalog.TryGet(id, out _), "Unsupported/native/custom skill received another skill's numbers: " + id);
Check(catalog.TryGet("injury", 0, 3, out var limp) && limp.Id == lion.Id && limp.Kind != lion.Kind && limp.Body.Contains("43.2%"), "Overlapping skill/injury IDs were conflated");
Check(catalog.TryGet("injury", 901, 1, out var minor) && minor.Body.Contains("16%") &&
    catalog.TryGet("injury", 901, 2, out var moderate) && moderate.Body.Contains("32%"), "Severity-specific pickup chances are wrong");
Check(!catalog.TryGet("injury", 901, 0, out _) && !catalog.TryGet("injury", 901, 3, out _), "Missing/invalid severity guessed another level");
Check(catalog.TryGet("injury", 510, 3, out var stamina) && stamina.Body.Contains("+75%") && stamina.Body.Contains("+15%"), "Fixed damage and severity-scaled stamina were confused");
Check(catalog.TryGet("purchase", 3008, 0, out var bow) && bow.Body.Contains("arrow base damage: 12") && bow.Body.Contains("5 base damage"), "Fired-arrow damage was confused with thrown-bow damage");
Check(catalog.TryGet("status", 8, 0, out var backstab) && backstab.Body.Contains("{backstab}"), "Backstab multiplier no longer follows the audited native getter");
Check(catalog.TryGet("status", 1, 0, out var vigor) && !vigor.Body.Contains("backstab", StringComparison.OrdinalIgnoreCase) &&
    !backstab.Body.Contains("stamina", StringComparison.OrdinalIgnoreCase), "Composing a combined skill mutated the standalone status cards");
Check(!catalog.TryGet("status", 9, 0, out _), "Combined status flags resolved to an unrelated single effect");
Check(catalog.TryGet(10007, out var followThrough) && followThrough.Body.Contains("20%") && followThrough.Body.Contains("15%"), "Implemented/localized stun-chance discrepancy was lost");
Check(!catalog.TryGet("injury", -30, 3, out _) && !catalog.TryGet("purchase", 2402, 0, out _), "Concealed/unreleased effects received a card");

// Character select supplies the displayed localization key, including unlock
// requirements for locked choices. Never parse translated text or infer ranks.
foreach (var (term, id) in new[] { ("Skills/Beast_Lion_Lv1_Desc", 0),
    ("Skills/Beast_Eagle_Lv1_Desc", 3), ("Skills/Beast_Bear_Lv2_Desc", 7) })
    Check(catalog.TryGetTerm(term, out var entry) && entry.Id == id && entry.Kind == "skill", "Wrong character-select term mapping: " + term);
foreach (var term in new string?[] { null, "", "Unlocks/Beast_Eagle_1_Req", "UI/BeastUnknownName",
    "Skills/Beast_Eagle_Lv1_Name", "Injuries/DropOnPickup_Desc_0", "Injuries/DropOnPickup_Desc_3",
    "Skills/Beast_Eagle_Lv1_Desc_Extra", "Powerful throw after melee kills" })
{
    catalog.TryGetTerm("Skills/Beast_Eagle_Lv1_Desc", out var previous);
    Check(!catalog.TryGetTerm(term, out previous) && previous == null, "Locked/unknown choice retained the old Eagle card: " + term);
}
using (var document = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "details.json"))))
{
    foreach (var row in document.RootElement.GetProperty("entries").EnumerateArray())
    {
        var kind = row.GetProperty("kind").GetString()!;
        var id = row.GetProperty("id").GetInt32();
        var severity = row.GetProperty("severity").GetInt32();
        Check(catalog.TryGet(kind, id, severity, out var detail), "Unreachable entry");
        Check(detail.Body.Length < 1000 && detail.Name.Length < 100, "Card needs editorial shortening");
        foreach (var key in row.GetProperty("terms").EnumerateArray())
            Check(catalog.TryGetTerm(key.GetString(), out var termDetail) && ReferenceEquals(detail, termDetail), "Typed and exact-term lookups disagree");
    }
}
foreach (var invalid in new[]
{
    "{\"entries\":[{\"kind\":\"injury\",\"id\":901,\"severity\":0,\"name\":\"Bad\",\"details\":[\"bad\"],\"terms\":[\"x\"]}]}",
    "{\"entries\":[{\"kind\":\"skill\",\"id\":0,\"severity\":0,\"name\":\"Bad\",\"details\":[\"\"],\"terms\":[\"x\"]}]}"
})
{
    bool rejected = false;
    try { using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalid)); _ = new DetailCatalog(stream); }
    catch (InvalidDataException) { rejected = true; }
    Check(rejected, "Malformed catalog silently accepted");
}

var cases = new[]
{
    (new Bounds(100, 300, 300, 500), 1200f, 800f, "right"),
    (new Bounds(900, 300, 1180, 500), 1200f, 800f, "left"),
    (new Bounds(100, 350, 700, 550), 800f, 600f, "below"),
    (new Bounds(100, 50, 700, 200), 800f, 600f, "above"),
};
foreach (var (anchor, width, height, side) in cases)
{
    var p = PanelPlacement.Place(anchor, 300, 200, width, height);
    Check(p.Left >= 16 && p.Left + 300 <= width - 16 && p.Top <= height - 16 && p.Top - 200 >= 16, "Card escapes screen: " + side);
    Check(p.Left >= anchor.Right || p.Left + 300 <= anchor.Left || p.Top <= anchor.Bottom || p.Top - 200 >= anchor.Top, "Card overlaps description despite free space: " + side);
}
// Moving windows and edge-aligned native panels must not leave the numeric card off-screen.
for (var w = 640; w <= 3840; w += 160)
for (var h = 480; h <= 2160; h += 120)
for (var edge = 0; edge <= 4; edge++)
{
    var anchor = new Bounds(w * edge / 4f - 60, h / 2f, w * edge / 4f + 60, h / 2f + 100);
    var p = PanelPlacement.Place(anchor, 300, 200, w, h);
    Check(p.Left >= 16 && p.Left + 300 <= w - 16 && p.Top <= h - 16 && p.Top - 200 >= 16, "Edge placement escaped viewport");
}
Console.WriteLine($"PASS: {catalog.Count} typed cards and exact description keys, namespace/severity isolation, locked/unknown choice clearing, evidence-sensitive examples, invalid-data rejection, four placements and 1,575 viewport/edge combinations. No Unity/gameplay test performed.");
