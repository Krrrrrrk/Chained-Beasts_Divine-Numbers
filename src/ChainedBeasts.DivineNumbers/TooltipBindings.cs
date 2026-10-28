using HarmonyLib;
using Il2CppInterop.Common;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ChainedBeasts.DivineNumbers;

internal static class TooltipBindings
{
    internal static DetailsController? Controller;
    internal static void Install(Harmony harmony)
    {
        Patch(harmony, typeof(UIPlayerInfoTooltip), "Initialise", new[] { typeof(SkillType), typeof(bool) }, nameof(Skill));
        Patch(harmony, typeof(UIPlayerInfoTooltip), "Initialise", new[] { typeof(InjuryType), typeof(int), typeof(bool) }, nameof(Injury));
        Patch(harmony, typeof(UIPlayerInfoTooltip), "Initialise", new[] { typeof(PurchaseType), typeof(bool) }, nameof(Purchase));
        Patch(harmony, typeof(UIPlayerInfoTooltip), "Initialise", new[] { typeof(StatusTooltipType) }, nameof(Status));
        Patch(harmony, typeof(UILobbyTrainerUpgradePage), "SetSkill", new[] { typeof(SkillType), typeof(bool) }, nameof(Trainer));
        Patch(harmony, typeof(UIGameLevelLobbyCharacterSkillInfo), "Initialise", new[] { typeof(SkillType), typeof(int) }, nameof(CharacterSkill));
        Patch(harmony, typeof(UIGameServerLobbyPage), "Refresh", new[] { typeof(GameServerLobby.UIState), typeof(int) }, nameof(CharacterSelect));
    }
    private static void Patch(Harmony harmony, Type type, string name, Type[] signature, string postfix)
    {
        var method = AccessTools.DeclaredMethod(type, name, signature) ?? throw new MissingMethodException(type.FullName, name);
        var entry = NativeEntry(method);
        if (entry == IntPtr.Zero) throw new InvalidOperationException("No native UI method entry: " + method);
        // IL2CPP can merge unrelated wrappers into one native function. Refuse a
        // detour that would spill over into another already-patched method.
        foreach (var other in Harmony.GetAllPatchedMethods())
            if (other != method && NativeEntry(other) == entry)
                throw new InvalidOperationException("Aliased UI method: " + method + " / " + other);
        harmony.Patch(method, postfix: new HarmonyMethod(typeof(TooltipBindings), postfix));
    }
    private static IntPtr NativeEntry(MethodBase method)
    {
        var field = Il2CppInteropUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
        if (field == null || field.GetValue(null) is not IntPtr info || info == IntPtr.Zero) return IntPtr.Zero;
        return Marshal.ReadIntPtr(info);
    }
    // Positional Harmony arguments avoid reliance on generated parameter names.
    private static void Skill(UIPlayerInfoTooltip __instance, SkillType __0) => Controller?.Bind(__instance, (int)__0);
    private static void Injury(UIPlayerInfoTooltip __instance, InjuryType __0, int __1) => Controller?.Bind(__instance, "injury", (int)__0, __1);
    private static void Purchase(UIPlayerInfoTooltip __instance, PurchaseType __0) => Controller?.Bind(__instance, "purchase", (int)__0, 0);
    private static void Status(UIPlayerInfoTooltip __instance, StatusTooltipType __0) => Controller?.Bind(__instance, "status", (int)__0, 0);
    private static void Trainer(UILobbyTrainerUpgradePage __instance, SkillType __0) => Controller?.Bind(__instance, (int)__0);
    private static void CharacterSkill(UIGameLevelLobbyCharacterSkillInfo __instance, SkillType __0) => Controller?.Bind(__instance, (int)__0);
    private static void CharacterSelect(UIGameServerLobbyPage __instance) => Controller?.Bind(__instance);
}
