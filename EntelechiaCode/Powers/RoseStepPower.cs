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

namespace Entelechia.EntelechiaCode.Powers;

// ponytail: next-attack-applies-BloodHarvest buff; self-removes after one attack hits
public class RoseStepPower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    private bool _triggered;
    private Creature? _pendingTarget;
    private CardModel? _pendingCard;
    private int _pendingPlayIndex;

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer, DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (dealer != Owner) return Task.CompletedTask;
        if (!EntelechiaDamage.IsAttackDamage(props, cardSource)) return Task.CompletedTask;
        if (_triggered) return Task.CompletedTask;

        _triggered = true;
        _pendingTarget = target;
        _pendingCard = cardSource;
        _pendingPlayIndex = cardSource!.CurrentPlayIndex;
        return Task.CompletedTask;
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (!_triggered || _pendingTarget == null || _pendingCard == null) return;
        if (!ReferenceEquals(cardPlay.Card, _pendingCard) || cardPlay.PlayIndex != _pendingPlayIndex) return;

        if (_pendingTarget.CurrentHp > 0)
            await CommonActions.Apply<BloodHarvestPower>(choiceContext, _pendingTarget, null, Amount, false);
        await PowerCmd.Remove(this);
    }
}
