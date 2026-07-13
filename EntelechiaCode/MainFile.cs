using Godot;
using Godot.Bridge;
using HarmonyLib;

using System.Reflection;
using System.Collections.Generic;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;

namespace Entelechia.EntelechiaCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "Entelechia";
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    // ponytail: single shared instance; avoids duplicate registration
    internal static Character.Entelechia? CharInstance { get; private set; }

    public static void Initialize()
    {
        ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());

        Logger.Info("Entelechia mod initialized");

        bool ordered = CustomCharacterUtils.TryOrderCustomCharacters(
            [typeof(Character.Entelechia)]);
        Logger.Info($"TryOrderCustomCharacters result: {ordered}");

        var harmony = new Harmony(ModId);
        harmony.PatchAll();

        // Fallback: if BaseLib's ModelDb patch didn't fire (v0.108.0 compat issue),
        // inject character directly so it appears in char select.
        ApplyModelDbFallback(harmony);
        ApplyTransitionPathFallback(harmony);
    }

    private static void ApplyTransitionPathFallback(Harmony harmony)
    {
        try
        {
            // "previously failed" = asset cached as failed during startup preload.
            // Patch the loader itself so the redirect happens before any cache hit.
            var loadAsset = AccessTools.Method("MegaCrit.Sts2.Core.Assets.AssetCache:LoadAsset");
            if (loadAsset == null) { Logger.Info("AssetCache.LoadAsset not found"); return; }
            harmony.Patch(loadAsset, prefix: new HarmonyMethod(typeof(MainFile), nameof(AssetLoadPrefix)));
            Logger.Info("AssetCache.LoadAsset transition redirect applied");
        }
        catch (Exception e) { Logger.Info($"Transition patch failed: {e.Message}"); }
    }

    private const string EntelechiaTransitionPath = "res://materials/transitions/entelechia_transition_mat.tres";
    private const string IroncladTransitionPath = "res://materials/transitions/ironclad_transition_mat.tres";
    private const string EntelechiaCombatVisualsPath = "res://scenes/creature_visuals/entelechia.tscn";
    private const string IroncladCombatVisualsPath = "res://scenes/creature_visuals/ironclad.tscn";

    // Redirect only the two legacy placeholder entry points before cache lookup.
    public static void AssetLoadPrefix(ref string path)
    {
        path = path switch
        {
            EntelechiaTransitionPath => IroncladTransitionPath,
            EntelechiaCombatVisualsPath => IroncladCombatVisualsPath,
            _ => path
        };
    }

    public static void FadeOutPrefix(ref string transitionPath)
    {
        if (transitionPath?.Contains("entelechia") == true)
            transitionPath = transitionPath.Replace("entelechia", "ironclad");
    }

    private static void ApplyModelDbFallback(Harmony harmony)
    {
        try
        {
            CharInstance = new Character.Entelechia();
            Logger.Info($"Entelechia instance created for ModelDb fallback, Id={CharInstance.Id}");

            var getter = AccessTools.PropertyGetter(typeof(ModelDb), nameof(ModelDb.AllCharacters));
            var postfix = new HarmonyMethod(typeof(MainFile), nameof(AllCharactersPostfix));
            harmony.Patch(getter, postfix: postfix);
            Logger.Info("ModelDb.AllCharacters fallback patch applied");
        }
        catch (Exception e)
        {
            Logger.Info($"ModelDb fallback patch failed: {e.Message}");
        }
    }

    // ponytail: appends our char if BaseLib didn't inject it already
    public static IEnumerable<CharacterModel> AllCharactersPostfix(IEnumerable<CharacterModel> __result)
    {
        if (CharInstance == null) return __result;
        foreach (var c in __result)
            if (c is Character.Entelechia) return __result; // already there
        Logger.Info("Injecting Entelechia into ModelDb.AllCharacters");
        return [.. __result, CharInstance];
    }
}
