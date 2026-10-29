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
Check(catalog.TryGet("purchase", 3000, 0, out var club) && club.Body.Contains("melee damage: 10") && club.Body.Contains("impact damage: 15"), "Merchant's typed Wooden Club purchase does not resolve");
Check(catalog.TryGetTerm("Injuries/ColourBlindness_Desc_1", out var colorBlindness) && colorBlindness.Body.Contains("66%"), "Training affliction preview lost its exact severity card");
Check(catalog.TryGet(400, out var noMercy) && noMercy.Name == "No Mercy" && noMercy.Body.Contains("{backstab}"), "Learned-skill No Mercy lookup lost its backstab explanation");
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
// Exercise real input sequences, including held buttons across UI/focus changes.
// Serialized shop prefab: OffsetContent sign is 368x406 at (-125,250).
// The separate 250x350 potion preview is at (0,135). The old sign-only
// hitbox misses both the preview's right edge and its bottom/pedestal area.
var sign = new Bounds(-309, 47, 59, 453);
var potionPreview = new Bounds(-125, -40, 125, 310);
var completeOffer = PanelPlacement.Union(sign, potionPreview);
foreach (var (x, y) in new[] { (-300f, 450f), (-300f, 60f), (100f, 0f), (120f, 200f), (-20f, 200f) })
{
    Check(PanelPlacement.Contains(completeOffer, x, y), "Complete offer misses header/blank space/price/potion");
    var scaled = new Bounds(completeOffer.Left * 2 + 1000, completeOffer.Bottom * 2 + 300,
        completeOffer.Right * 2 + 1000, completeOffer.Top * 2 + 300);
    Check(PanelPlacement.Contains(scaled, x * 2 + 1000, y * 2 + 300), "Scaled offer hover misses the same visible point");
}
Check(!PanelPlacement.Contains(sign, 100, 0), "Regression fixture no longer distinguishes sign-only hover");
Check(!PanelPlacement.Contains(completeOffer, 130, 0) && !PanelPlacement.Contains(completeOffer, -310, 100),
    "Offer hitbox extends into unrelated UI");

// The HEALER has a different component and different positions from the shop.
// UILobbyDoctorSelectionPage: sign (-145,35), preview (0,-80).
var healerSign = new Bounds(-329, -168, 39, 238);
var healerPreview = new Bounds(-125, -255, 125, 95);
var healerOffer = PanelPlacement.Union(healerSign, healerPreview);
foreach (var (x, y) in new[] { (-320f, 230f), (-320f, 0f), (-200f, 0f), (0f, -160f), (100f, -220f) })
    Check(PanelPlacement.Contains(healerOffer, x, y), "Healer header/frame/text/healing/model point missed");
Check(!PanelPlacement.Contains(healerSign, 100, -220) && PanelPlacement.Contains(healerOffer, 100, -220),
    "Healer model regression no longer distinguishes sign-only hover");
Check(!PanelPlacement.Contains(healerOffer, 126, -220) && !PanelPlacement.Contains(healerOffer, -330, 0),
    "Healer bounds escaped into adjacent UI");

var toggle = new NumbersToggle();
// Reproduce the reported healer screen with a visible Ibex HUD underneath it.
var healer = new InspectionSelection<string>();
healer.Consider("Agile Ibex HUD", 1, hud: true);
healer.Consider("Dislocated Shoulder II potion", 10, offer: true);
Check(healer.Selected(controller: true) == "Dislocated Shoulder II potion", "Focused healer potion lost to HUD");
var unknownPotion = new InspectionSelection<string>();
unknownPotion.Consider("Agile Ibex HUD", 1, hud: true);
unknownPotion.Consider("Concealed/no-card potion", 10, offer: true);
Check(unknownPotion.Selected(controller: true) == "Concealed/no-card potion", "Unsupported offer fell back to beast numbers");
var noFocus = new InspectionSelection<string>();
noFocus.Consider("Agile Ibex HUD", 1, hud: true);
noFocus.Consider("Unfocused healer potion", 0, offer: true);
Check(noFocus.Selected(controller: true) == null, "Healer with no focused offer displayed unrelated HUD numbers");
Check(noFocus.Selected(controller: false) == "Agile Ibex HUD", "Controller-only HUD suppression changed keyboard inspection");
var twoPlayers = new InspectionSelection<string>();
twoPlayers.Consider("Other player's potion", 0, offer: true);
twoPlayers.Consider("Owning player's potion", 10, offer: true);
Check(twoPlayers.Selected(controller: true) == "Owning player's potion", "Another player's unfocused potion won selection");
foreach (var menu in new[] { "merchant", "training affliction", "learned-skill choices" })
{
    var focusedMenu = new InspectionSelection<string>();
    focusedMenu.Consider("Desperate Ibex HUD", 1, hud: true);
    focusedMenu.Consider(menu, 10, offer: true);
    Check(focusedMenu.Selected(controller: true) == menu, "Modal controller selection lost to HUD: " + menu);
    var unfocusedMenu = new InspectionSelection<string>();
    unfocusedMenu.Consider("Desperate Ibex HUD", 1, hud: true);
    unfocusedMenu.Consider(menu, 0, offer: true);
    Check(unfocusedMenu.Selected(controller: true) == null, "Unfocused modal fell back to beast numbers: " + menu);
}
void Frame(bool context = true, bool focused = true, bool ctrl = false,
    bool down = false, bool connected = true, bool secondDown = false, bool secondConnected = false)
{
    var buttons = new List<ControllerButtonState>();
    if (connected) buttons.Add(new(11, down));
    if (secondConnected) buttons.Add(new(22, secondDown));
    toggle.Update(focused, context, ctrl, buttons);
}
void IsOff(string reason) => Check(toggle.ControllerDeviceId == null && !toggle.Requested, reason);
Frame(down: true);
Check(!toggle.IsNewPress(new(11, true)), "Held R3 is treated as a fresh HUD-inspection request");
Check(toggle.Requested && toggle.ControllerDeviceId == 11, "Stick click did not latch");
for (var i = 0; i < 120; i++) Frame(down: true);
Check(toggle.Requested, "Held stick repeated the toggle");
Frame();
Check(toggle.Requested, "Releasing the stick cleared the latch");
Frame(down: true);
IsOff("Second press did not turn numbers off");
Frame(context: false);
Frame(context: false, down: true);
Frame(down: true);
IsOff("Holding the button while opening a menu armed the toggle");
Frame(); Frame(down: true);
Check(toggle.Requested, "Fresh press after opening the menu did not work");
Frame(context: false, down: true);
Frame(down: true);
IsOff("Closing/reopening a menu retained the toggle");
Frame(); Frame(down: true);
Frame(focused: false, down: true);
Frame(down: true);
IsOff("Losing/regaining application focus retained or rearmed the toggle");
Frame(); Frame(down: true);
Frame(connected: false);
IsOff("Disconnecting the owner retained the toggle");
Frame(); Frame(down: true);
Frame(secondConnected: true, secondDown: true);
Check(toggle.ControllerDeviceId == 22, "Another controller could not take ownership");
Frame(secondConnected: true);
Frame(secondConnected: true, secondDown: true, ctrl: true);
Check(toggle.ControllerDeviceId == null && toggle.Requested, "Controller off incorrectly hid held Ctrl");
Frame(ctrl: true, connected: false, context: false);
Check(toggle.Requested, "Keyboard Ctrl needs a connected controller or cached UI context");
Frame(connected: false);
IsOff("Releasing Ctrl retained an unlatched overlay");
Frame(down: true);
toggle.Reset();
IsOff("Cleanup retained controller state");

Console.WriteLine($"PASS: {catalog.Count} typed cards and exact description keys, namespace/severity isolation, locked/unknown choice clearing, evidence-sensitive examples, invalid-data rejection, four placements and 1,575 viewport/edge combinations; distinct shop/healer hover geometry; healer/merchant/trainer modal selection and no-focus HUD suppression; controller press/hold/release, menu/focus reset, disconnect, ownership and keyboard coexistence sequences. No Unity/gameplay test performed.");
