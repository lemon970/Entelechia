using Entelechia.EntelechiaCode.Cards;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace Entelechia.EntelechiaCode;

[HarmonyPatch(typeof(CardModel), nameof(CardModel.UpgradeInternal))]
public static class EnchantmentUpgradePatch
{
    [HarmonyPostfix]
    public static void RefreshEnchantedValues(CardModel __instance)
    {
        if (__instance is not EntelechiaCard || __instance.Enchantment is null)
            return;

        foreach (var dynamicVar in __instance.DynamicVars.Values)
        {
            dynamicVar.UpdateCardPreview(
                __instance,
                CardPreviewMode.None,
                target: null,
                runGlobalHooks: false);
        }
    }
}

[HarmonyPatch(typeof(CardModel), nameof(CardModel.EnchantInternal))]
public static class EnchantmentAppliedPatch
{
    [HarmonyPostfix]
    public static void Postfix(CardModel __instance)
        => EnchantmentUpgradePatch.RefreshEnchantedValues(__instance);
}
