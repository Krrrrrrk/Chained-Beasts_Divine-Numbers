using ChainedBeasts.DivineNumbers;

static void Check(bool value, string message)
{ if (!value) throw new InvalidOperationException(message); }

using var input = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "details.json"));
var catalog = new DetailCatalog(input);
Check(catalog.TryGet(0, out var lion) && lion.Body.Contains("+40%") && lion.Body.Contains("10 seconds"), "Lion card missing researched numbers");
Check(catalog.TryGet(3, out var eagle) && eagle.Body.Contains("8 seconds") && eagle.Body.Contains("Consumed"), "Eagle card loses the ready-window/consumption distinction");
Check(catalog.TryGet(7, out var bear) && bear.Body.Contains("minimum 0.3") && bear.Body.Contains("preserved"), "Bear card treats minimum stun as extra stun");
foreach (var id in new[] { -1, 1, 2, 4, 6, 8, 11010, int.MaxValue })
    Check(!catalog.TryGet(id, out _), "Unsupported/native/custom skill received another skill's numbers: " + id);

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
Console.WriteLine("PASS: audited card mappings, unsupported IDs, four placement directions, and 1,575 viewport/edge combinations. No Unity/gameplay test performed.");
