using System.Reflection;
using System.Runtime.ExceptionServices;
using Entelechia.EntelechiaCode.Cards;

internal static class HextechRunesCompatibilityExpectations
{
    internal static void Run(List<string> failures)
    {
        var assembly = typeof(EntelechiaCard).Assembly;
        var compatibilityType = assembly.GetType(
            "Entelechia.EntelechiaCode.HextechRunesCompatibility",
            throwOnError: true)!;
        var matcher = compatibilityType.GetMethod(
            "IsBrokenBetaDamageCall",
            BindingFlags.NonPublic | BindingFlags.Static)!;

        var hexFailure = new MissingMethodException(
            "Method not found: 'MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(...)'.");
        ExceptionDispatchInfo.SetRemoteStackTrace(
            hexFailure,
            "   at HextechRunes.HextechGameApiCompat.Damage(...)\n");

        if (matcher.Invoke(null, [hexFailure]) is not true)
            failures.Add("Hextech compatibility did not recognize the beta Damage API failure");

        var unrelatedFailure = new MissingMethodException(
            "Method not found: 'MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage(...)'.");
        ExceptionDispatchInfo.SetRemoteStackTrace(
            unrelatedFailure,
            "   at AnotherMod.GameApiCompat.Damage(...)\n");

        if (matcher.Invoke(null, [unrelatedFailure]) is not false)
            failures.Add("Hextech compatibility suppressed an unrelated MissingMethodException");
    }
}
