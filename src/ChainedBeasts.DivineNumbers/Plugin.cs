using System.Security.Cryptography;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace ChainedBeasts.DivineNumbers;

[BepInPlugin(Id, Name, Version)]
public sealed class Plugin : BasePlugin
{
    public const string Id = "local.chainedbeasts.divinenumbers";
    public const string Name = "Chained Beasts Divine Numbers";
    public const string Version = "0.1.1";
    internal const string GameHash = "ba3ef02fa87da4c41bb27d7610539c9578132204f0fb5713833e2ef93415ce29";
    private Harmony? _harmony;
    private DetailsController? _controller;
    public override void Load()
    {
        if (!Config.Bind("Display", "Enabled", true, "Show the separate numbers box while either Ctrl key is held. Restart to apply.").Value)
            return;
        try
        {
            using var binary = File.OpenRead(Path.Combine(Paths.GameRootPath, "GameAssembly.dll"));
            using var algorithm = SHA256.Create();
            var hash = Convert.ToHexString(algorithm.ComputeHash(binary)).ToLowerInvariant();
            if (hash != GameHash)
                throw new NotSupportedException("Game build has changed. Numeric details need a fresh audit. SHA256=" + hash);
            using var data = typeof(Plugin).Assembly.GetManifestResourceStream("NumericDetails.json")
                ?? throw new InvalidDataException("Numeric details resource is missing");
            var catalog = new DetailCatalog(data);
            _controller = AddComponent<DetailsController>();
            _controller.Configure(catalog, Log);
            TooltipBindings.Controller = _controller;
            _harmony = new Harmony(Id);
            TooltipBindings.Install(_harmony);
            Log.LogInfo("Divine Numbers 0.1.1: hold Ctrl for researched numbers, including character select. Compact 22-point cards; updated UI needs manual verification.");
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
