using System.Reflection;
using Entelechia.EntelechiaCode.Relics;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode;

[HarmonyPatch]
public static class BloodDemonRepleteDamageCompatibilityPatch
{
    public static MethodBase TargetMethod()
    {
        return typeof(RelicModel).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(method =>
                method.Name == nameof(AbstractModel.ModifyDamageMultiplicative)
                && method.ReturnType == typeof(decimal)
                && method.GetParameters().Length is 5 or 6);
    }

    public static void Postfix(
        AbstractModel __instance,
        Creature? target,
        ValueProp props,
        CardModel? cardSource,
        ref decimal __result)
    {
        if (__instance is BloodDemonReplete relic)
            __result *= relic.GetDamageMultiplier(target, props, cardSource);
    }
}
