using System.Security.Cryptography;
using System.Text.Json;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ChainedBeasts.DivineNumbers;

[BepInPlugin(Id, Name, Version)]
public sealed class Plugin : BasePlugin
{
    public const string Id = "local.chainedbeasts.divinenumbers";
    public const string Name = "Chained Beasts Divine Numbers";
    public const string Version = "0.2.3";
    internal const string GameHash = "ba3ef02fa87da4c41bb27d7610539c9578132204f0fb5713833e2ef93415ce29";
    private Harmony? _harmony;
    private DetailsController? _controller;
    public override void Load()
    {
        if (!Config.Bind("Display", "Enabled", true, "Enable the separate numbers box (hold Ctrl or toggle with a controller). Restart to apply.").Value)
            return;
        var controllerButton = Config.Bind("Input", "ControllerToggleButton", ControllerToggleButton.RightStickClick,
            "Controller numbers toggle: RightStickClick (R3), LeftStickClick (L3), or Disabled. Restart to apply.").Value;
        try
        {
            using var binary = File.OpenRead(Path.Combine(Paths.GameRootPath, "GameAssembly.dll"));
            using var algorithm = SHA256.Create();
            var hash = Convert.ToHexString(algorithm.ComputeHash(binary)).ToLowerInvariant();
            if (hash != GameHash)
                throw new NotSupportedException("Game build has changed. Numeric details need a fresh audit. SHA256=" + hash);
            using var audit = typeof(Plugin).Assembly.GetManifestResourceStream("AuditEvidence.json")
                ?? throw new InvalidDataException("Audit fingerprint resource is missing");
            using var evidence = JsonDocument.Parse(audit);
            foreach (var file in evidence.RootElement.GetProperty("gameFiles").EnumerateArray())
            {
                var path = file.GetProperty("path").GetString()!;
                if (path == "GameAssembly.dll") continue;
                using var asset = File.OpenRead(Path.Combine(Paths.GameRootPath, path));
                var actual = Convert.ToHexString(algorithm.ComputeHash(asset));
                if (!actual.Equals(file.GetProperty("sha256").GetString(), StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException("Game data has changed; numeric details need a fresh audit: " + path);
            }
            using var data = typeof(Plugin).Assembly.GetManifestResourceStream("NumericDetails.json")
                ?? throw new InvalidDataException("Numeric details resource is missing");
            var catalog = new DetailCatalog(data);
            _controller = AddComponent<DetailsController>();
            _controller.Configure(catalog, Log, controllerButton);
            TooltipBindings.Controller = _controller;
            _harmony = new Harmony(Id);
            TooltipBindings.Install(_harmony);
            Log.LogInfo($"Divine Numbers {Version}: {catalog.Count} audited cards. Hold Ctrl or toggle numbers with {controllerButton}. Controller UI needs manual verification.");
        }
        catch (Exception ex)
        {
            Log.LogError("Divine Numbers disabled: " + ex);
            Unload();
        }
    }
    public override bool Unload()
    {
        TooltipBindings.Controller = null;
        _harmony?.UnpatchSelf();
        if (_controller != null)
        {
            _controller.Cleanup();
            UnityEngine.Object.Destroy(_controller);
        }
        return true;
    }
}
