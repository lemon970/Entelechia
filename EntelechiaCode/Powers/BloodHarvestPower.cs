using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;
using System.Threading;

namespace Entelechia.EntelechiaCode.Powers;

// Debuff on enemy. Each attack hit consumes one stack and heals the attacker.
public class BloodHarvestPower : EntelechiaPower
{
    private const decimal HealPerHit = 3m;
    private const decimal MaxHealPerCardPlay = 6m;

    private static readonly CardPlayHealKeyComparer PlayKeyComparer = new();
    private static readonly Dictionary<CardPlayHealKey, decimal> HealedByPlay = new(PlayKeyComparer);
    private static readonly Dictionary<CardPlayHealKey, int> TriggeredByPlay = new(PlayKeyComparer);
    private static readonly AsyncLocal<CardModel?> SuppressedCardSource = new();

    public override PowerType Type => PowerType.Debuff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public static void ResetCardPlayHealLedger()
    {
        HealedByPlay.Clear();
        TriggeredByPlay.Clear();
    }

    public static int GetTriggerCountForCurrentCardPlay(CardModel? cardSource)
    {
        if (cardSource == null) return 0;
        return TriggeredByPlay
            .Where(entry => ReferenceEquals(entry.Key.Card, cardSource))
            .Sum(entry => entry.Value);
    }

    public static async Task RunWithoutTriggerForCard(CardModel cardSource, Func<Task> action)
    {
        var previous = SuppressedCardSource.Value;
        SuppressedCardSource.Value = cardSource;
        try
        {
            await action();
        }
        finally
        {
            SuppressedCardSource.Value = previous;
        }
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        RemoveCardPlayEntries(HealedByPlay, cardPlay.Card);
        RemoveCardPlayEntries(TriggeredByPlay, cardPlay.Card);
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (target != Owner || dealer == null) return;
        if (ReferenceEquals(SuppressedCardSource.Value, cardSource)) return;
        if (!EntelechiaDamage.IsAttackDamage(props, cardSource)) return;
        if (result.UnblockedDamage <= 0) return;
        if (Amount <= 0) return;

        RecordTriggerForCurrentCardPlay(cardSource!);

        var healAmount = ClaimHealForCurrentCardPlay(cardSource, HealPerHit);
        if (healAmount > 0)
            await TurnStateTracker.HealTracking(dealer, healAmount, false);

        var barrier = dealer.Powers?.FirstOrDefault(p => p is ClottingBarrierPower);
        if (barrier != null && barrier.Amount > 0)
            await CreatureCmd.GainBlock(dealer, barrier.Amount, default, null, false);

        var instinct = dealer.Powers?.FirstOrDefault(p => p is ClotInstinctPower) as ClotInstinctPower;
        if (instinct != null && !instinct.TriggeredThisTurn)
        {
            instinct.TriggeredThisTurn = true;
            await CreatureCmd.GainBlock(dealer, instinct.Amount, default, null, false);
        }

        if (Amount <= 1)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(choiceContext, this, -1, dealer, cardSource, false);
    }

    private static decimal ClaimHealForCurrentCardPlay(CardModel? cardSource, decimal amount)
    {
        if (cardSource == null) return amount;

        var key = new CardPlayHealKey(cardSource, cardSource.CurrentPlayIndex);
        HealedByPlay.TryGetValue(key, out var alreadyHealed);

        var granted = Math.Min(amount, Math.Max(MaxHealPerCardPlay - alreadyHealed, 0m));
        HealedByPlay[key] = alreadyHealed + granted;
        return granted;
    }

    private static void RecordTriggerForCurrentCardPlay(CardModel cardSource)
    {
        var key = new CardPlayHealKey(cardSource, cardSource.CurrentPlayIndex);
        TriggeredByPlay.TryGetValue(key, out var alreadyTriggered);
        TriggeredByPlay[key] = alreadyTriggered + 1;
    }

    private static void RemoveCardPlayEntries<TValue>(Dictionary<CardPlayHealKey, TValue> entries, CardModel card)
    {
        foreach (var key in entries.Keys.Where(key => ReferenceEquals(key.Card, card)).ToList())
            entries.Remove(key);
    }

    private readonly record struct CardPlayHealKey(CardModel Card, int PlayIndex);

    private sealed class CardPlayHealKeyComparer : IEqualityComparer<CardPlayHealKey>
    {
        public bool Equals(CardPlayHealKey x, CardPlayHealKey y)
        {
            return ReferenceEquals(x.Card, y.Card) && x.PlayIndex == y.PlayIndex;
        }

        public int GetHashCode(CardPlayHealKey obj)
        {
            return HashCode.Combine(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Card),
                obj.PlayIndex);
        }
    }
}
