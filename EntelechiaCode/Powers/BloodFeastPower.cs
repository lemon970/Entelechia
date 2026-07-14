using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Powers;

public class BloodFeastPower : EntelechiaPower
{
    private static readonly CardPlayHitKeyComparer HitKeyComparer = new();
    private readonly Dictionary<CardPlayHitKey, int> _countedHitsByPlay = new(HitKeyComparer);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public int HitCount { get; set; } = 0;
    public int Threshold { get; set; } = 4;
    public bool TriggeredThisTurn { get; set; }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource)
    {
        SetAmount(HitCount, false);
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature == Owner)
        {
            HitCount = 0;
            TriggeredThisTurn = false;
            _countedHitsByPlay.Clear();
            SetAmount(HitCount, false);
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        foreach (var key in _countedHitsByPlay.Keys.Where(key => ReferenceEquals(key.Card, cardPlay.Card)).ToList())
            _countedHitsByPlay.Remove(key);
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner) return;
        if (!EntelechiaDamage.IsAttackDamage(props, cardSource)) return;
        if (result.UnblockedDamage <= 0) return;
        RecordCountedHit(cardSource);

        if (result.WasTargetKilled || target.CurrentHp <= 0)
        {
            await CountLethalHit(choiceContext, cardSource);
            return;
        }

        await CountHit(choiceContext, target);
    }

    public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand attack)
    {
        await ReconcileCompletedAttack(choiceContext, attack);
    }

    public async Task ReconcileCompletedAttack(PlayerChoiceContext choiceContext, AttackCommand attack)
    {
        if (!TryGetBloodFeastCardSource(attack, out var cardSource)) return;

        var results = attack.Results.SelectMany(hit => hit).Where(result => result.UnblockedDamage > 0).ToList();
        if (results.Count == 0) return;

        var key = CreateHitKey(cardSource);
        _countedHitsByPlay.TryGetValue(key, out var counted);
        var missing = results.Count - counted;
        if (missing <= 0) return;

        foreach (var result in results.Where(result => result.WasTargetKilled).Take(missing))
        {
            RecordCountedHit(cardSource);
            await CountLethalHit(choiceContext, cardSource);
        }
    }

    public async Task ReconcileKilledTarget(PlayerChoiceContext choiceContext, AttackCommand attack, Creature target)
    {
        if (!TryGetBloodFeastCardSource(attack, out var cardSource)) return;
        if (target.CurrentHp > 0) return;

        var resultCount = attack.Results.SelectMany(hit => hit).Count(result => result.UnblockedDamage > 0);
        var expectedCount = Math.Max(resultCount, 1);
        var key = CreateHitKey(cardSource);
        _countedHitsByPlay.TryGetValue(key, out var counted);
        if (counted >= expectedCount) return;

        RecordCountedHit(cardSource);
        await CountLethalHit(choiceContext, cardSource);
    }

    private async Task CountHit(PlayerChoiceContext choiceContext, Creature target)
    {
        if (TriggeredThisTurn) return;
        HitCount++;
        SetAmount(HitCount, false);
        if (HitCount >= Threshold)
        {
            TriggeredThisTurn = true;
            await CommonActions.Apply<StrengthPower>(choiceContext, Owner, null, 1, false);
            if (target.CurrentHp > 0)
                await CommonActions.Apply<BloodHarvestPower>(choiceContext, target, null, 2, false);
        }
    }

    private async Task CountLethalHit(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        if (TriggeredThisTurn) return;
        HitCount++;
        SetAmount(HitCount, false);
        if (HitCount >= Threshold)
        {
            TriggeredThisTurn = true;
            await ApplyStrengthReliablyDuringLethal(choiceContext, cardSource);
        }
    }

    private async Task ApplyStrengthReliablyDuringLethal(PlayerChoiceContext choiceContext, CardModel? cardSource)
    {
        var before = Owner.Powers?.FirstOrDefault(power => power is StrengthPower)?.Amount ?? 0;
        await CommonActions.Apply<StrengthPower>(choiceContext, Owner, cardSource, 1, false);
        var after = Owner.Powers?.FirstOrDefault(power => power is StrengthPower)?.Amount ?? 0;
        if (after > before) return;

        var existing = Owner.Powers?.FirstOrDefault(power => power is StrengthPower);
        if (existing != null)
        {
            existing.SetAmount(existing.Amount + 1, false);
            return;
        }

        var strength = (StrengthPower)ModelDb.Power<StrengthPower>().MutableClone();
        strength.ApplyInternal(Owner, 1, false);
    }

    private void RecordCountedHit(CardModel? cardSource)
    {
        if (cardSource == null) return;
        var key = CreateHitKey(cardSource);
        _countedHitsByPlay.TryGetValue(key, out var counted);
        _countedHitsByPlay[key] = counted + 1;
    }

    private static CardPlayHitKey CreateHitKey(CardModel cardSource)
        => new(cardSource, cardSource.CurrentPlayIndex);

    private bool TryGetBloodFeastCardSource(AttackCommand attack, out CardModel cardSource)
    {
        cardSource = null!;
        if (attack.ModelSource is not CardModel source) return false;
        var attacker = attack.Attacker ?? source.Owner?.Creature;
        if (attacker != Owner) return false;
        if (source.Type != CardType.Attack) return false;

        cardSource = source;
        return true;
    }

    private readonly record struct CardPlayHitKey(CardModel Card, int PlayIndex);

    private sealed class CardPlayHitKeyComparer : IEqualityComparer<CardPlayHitKey>
    {
        public bool Equals(CardPlayHitKey x, CardPlayHitKey y)
        {
            return ReferenceEquals(x.Card, y.Card) && x.PlayIndex == y.PlayIndex;
        }

        public int GetHashCode(CardPlayHitKey obj)
        {
            return HashCode.Combine(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Card),
                obj.PlayIndex);
        }
    }
}
