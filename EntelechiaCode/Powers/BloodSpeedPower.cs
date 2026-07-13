using BaseLib.Abstracts;
using BaseLib.Utils;
using Entelechia.EntelechiaCode.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode.Powers;

// Buff on player. The next Amount attack cards hit one extra time.
public class BloodSpeedPower : EntelechiaPower
{
    private static readonly CardPlayTriggerKeyComparer TriggerKeyComparer = new();
    private readonly HashSet<CardPlayTriggerKey> _triggeredPlays = new(TriggerKeyComparer);

    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        _triggeredPlays.Remove(new CardPlayTriggerKey(cardPlay.Card, cardPlay.PlayIndex));
        return Task.CompletedTask;
    }

    public override async Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner) return;
        if (!EntelechiaDamage.IsAttackDamage(props, cardSource)) return;
        if (cardSource is not EntelechiaCard hiddenCard) return;
        if (hiddenCard.Type != CardType.Attack) return;
        if (hiddenCard.AttackBaseDamage <= 0m) return;
        if (result.UnblockedDamage <= 0m) return;
        if (Amount <= 0) return;

        var key = new CardPlayTriggerKey(cardSource, cardSource.CurrentPlayIndex);
        if (!_triggeredPlays.Add(key)) return;

        if (Amount <= 1)
            await PowerCmd.Remove(this);
        else
            await PowerCmd.ModifyAmount(choiceContext, this, -1, dealer, cardSource, false);

        if (target.CurrentHp <= 0) return;

        var extraDamage = Math.Max(2m, Math.Ceiling(hiddenCard.AttackBaseDamage * 0.35m));
        var extraPlay = new CardPlay
        {
            Card = hiddenCard,
            Target = target,
            ResultPile = PileType.Play,
            Resources = new ResourceInfo
            {
                EnergySpent = 0,
                EnergyValue = 0,
                StarsSpent = 0,
                StarValue = 0
            },
            IsAutoPlay = true,
            PlayIndex = cardSource.CurrentPlayIndex,
            PlayCount = 1
        };
        await new AttackCommand(extraDamage).FromCardCompatibility(hiddenCard, extraPlay).Targeting(target).WithHitCount(1).Execute(choiceContext);
    }

    private readonly record struct CardPlayTriggerKey(CardModel Card, int PlayIndex);

    private sealed class CardPlayTriggerKeyComparer : IEqualityComparer<CardPlayTriggerKey>
    {
        public bool Equals(CardPlayTriggerKey x, CardPlayTriggerKey y)
        {
            return ReferenceEquals(x.Card, y.Card) && x.PlayIndex == y.PlayIndex;
        }

        public int GetHashCode(CardPlayTriggerKey obj)
        {
            return HashCode.Combine(
                System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj.Card),
                obj.PlayIndex);
        }
    }
}
