namespace Entelechia.EntelechiaCode;

internal static class HextechRunesCompatibility
{
    private const string MissingDamageMethod =
        "MegaCrit.Sts2.Core.Commands.CreatureCmd.Damage";
    private const string HextechDamageAdapter =
        "HextechRunes.HextechGameApiCompat.Damage";

    private static int _warningLogged;

    internal static bool IsBrokenBetaDamageCall(Exception exception)
    {
        foreach (var candidate in Enumerate(exception))
        {
            if (candidate is MissingMethodException
                && candidate.Message.Contains(MissingDamageMethod, StringComparison.Ordinal)
                && candidate.StackTrace?.Contains(HextechDamageAdapter, StringComparison.Ordinal) == true)
                return true;
        }

        return false;
    }

    internal static void LogSuppressed(string source)
    {
        if (Interlocked.Exchange(ref _warningLogged, 1) != 0) return;

        MainFile.Logger.Info(
            $"[HextechRunes-Compatibility] Suppressed HextechRunes 0.8.5 beta Damage API failure at {source}. " +
            "The original heal/block completed; card resolution will continue.");
    }

    private static IEnumerable<Exception> Enumerate(Exception exception)
    {
        if (exception is AggregateException aggregate)
        {
            foreach (var inner in aggregate.Flatten().InnerExceptions)
            foreach (var candidate in Enumerate(inner))
                yield return candidate;
            yield break;
        }

        for (var current = exception; current != null; current = current.InnerException)
            yield return current;
    }
}
