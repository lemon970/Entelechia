using System.Reflection;
using System.Runtime.CompilerServices;
using BaseLib.Utils;
using Entelechia.EntelechiaCode.Cards;

internal static class Sts2CompatibilityExpectations
{
    internal static void Run(List<string> failures)
    {
        var fromCardResolver = typeof(BetaMainCompatibility)
            .GetField("_fromCard", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null);
        var fromCardParameterCount = (int?)fromCardResolver?.GetType()
            .GetProperty("ParamCount", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(fromCardResolver) ?? 0;
        if (fromCardParameterCount is not (1 or 2))
            failures.Add($"AttackCommand.FromCard compatibility resolved {fromCardParameterCount} parameters");

        var assembly = typeof(EntelechiaCard).Assembly;
        var compatibilityType = assembly.GetType(
            "Entelechia.EntelechiaCode.Sts2Compatibility",
            throwOnError: true)!;
        RuntimeHelpers.RunClassConstructor(compatibilityType.TypeHandle);

        var damageMethod = (MethodInfo?)compatibilityType
            .GetField("CreatureDamageMethod", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetValue(null);
        var damageParameterCount = damageMethod?.GetParameters().Length ?? 0;
        if (damageParameterCount is not (6 or 7))
            failures.Add($"CreatureCmd.Damage compatibility resolved {damageParameterCount} parameters");

        var patchType = assembly.GetType(
            "Entelechia.EntelechiaCode.CrimsonWardDamageCompatibilityPatch",
            throwOnError: true)!;
        var targetMethod = (MethodInfo?)patchType
            .GetMethod("TargetMethod", BindingFlags.Public | BindingFlags.Static)?
            .Invoke(null, null);
        var hookParameterCount = targetMethod?.GetParameters().Length ?? 0;
        if (hookParameterCount is not (5 or 6))
            failures.Add($"Crimson Ward damage hook resolved {hookParameterCount} parameters");
    }
}
