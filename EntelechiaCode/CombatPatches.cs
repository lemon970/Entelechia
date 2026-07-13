using HarmonyLib;
using BaseLib.Extensions;
using Entelechia.EntelechiaCode.Animation;
using System.Reflection;
using Entelechia.EntelechiaCode.Powers;
using Entelechia.EntelechiaCode.Cards;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode;

/// <summary>
/// Tracks HP change events within the current turn for conditional card effects.
/// </summary>
public static class TurnStateTracker
{
    public static bool LostHpThisTurn;
    public static bool HealedThisTurn;
    public static decimal HpLostThisTurn;
    public static decimal HpHealedThisTurn;

    public static void Reset()
    {
        LostHpThisTurn = false;
        HealedThisTurn = false;
        HpLostThisTurn = 0m;
        HpHealedThisTurn = 0m;
    }

    public static bool IsPlayerCreature(Creature? creature)
    {
        return creature?.CombatState?.PlayerCreatures?.Contains(creature) == true;
    }

    public static bool IsOwnerPlayerTurn(Creature? creature)
    {
        if (!IsPlayerCreature(creature)) return false;
        var combatState = creature!.CombatState;
        if (combatState?.CurrentSide != CombatSide.Player) return false;

        var player = creature.Player;
        if (player == null) return false;

        return CombatManager.Instance.IsPartOfPlayerTurn(player);
    }

    public static async Task SetCurrentHpTracking(Creature creature, decimal hp)
    {
        var before = creature.CurrentHp;
        await CreatureCmd.SetCurrentHp(creature, hp);
        TrackHpDelta(creature, creature.CurrentHp - before);
    }

    public static async Task HealTracking(Creature creature, decimal amount, bool affectsGameplay)
    {
        var before = creature.CurrentHp;
        await CreatureCmd.Heal(creature, amount, affectsGameplay);
        TrackHpDelta(creature, creature.CurrentHp - before);
    }

    public static async Task LoseHpTracking(
        PlayerChoiceContext? context,
        Creature creature,
        decimal amount,
        ValueProp props,
        Creature? dealer = null,
        CardModel? cardSource = null,
        CardPlay? cardPlay = null)
    {
        if (amount <= 0) return;

        var before = creature.CurrentHp;
        await Sts2Compatibility.Damage(
            context ?? new BlockingPlayerChoiceContext(),
            creature,
            amount,
            props,
            dealer,
            cardSource,
            cardPlay);
        TrackHpDelta(creature, creature.CurrentHp - before);
    }

    private static void TrackHpDelta(Creature creature, decimal delta)
    {
        if (!IsPlayerCreature(creature) || delta == 0) return;
        if (delta < 0)
        {
            LostHpThisTurn = true;
            HpLostThisTurn += -delta;
        }
        else
        {
            HealedThisTurn = true;
            HpHealedThisTurn += delta;
        }
    }
}

[HarmonyPatch]
public static class CrimsonWardDamageCompatibilityPatch
{
    public static MethodBase TargetMethod()
    {
        return typeof(PowerModel).GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Single(method =>
                method.Name == nameof(PowerModel.ModifyDamageMultiplicative)
                && method.ReturnType == typeof(decimal)
                && method.GetParameters().Length is 5 or 6);
    }

    public static void Postfix(
        PowerModel __instance,
        Creature? target,
        decimal amount,
        ValueProp props,
        CardModel? cardSource,
        ref decimal __result)
    {
        if (__instance is CrimsonWardPower ward)
            __result *= ward.GetDamageMultiplier(target, amount, props, cardSource);
    }
}

public static class HeartCandleLedger
{
    private static readonly Dictionary<Creature, decimal> OriginalConsumedByCreature = new();
    private static readonly Dictionary<Creature, decimal> OutstandingRevivedByCreature = new();
    private static readonly Dictionary<Creature, decimal> TotalRevivedByCreature = new();
    private static readonly Dictionary<Creature, decimal> NaturalGeneratedByCreature = new();
    private static readonly Dictionary<Creature, decimal> TotalConsumedByCreature = new();

    public static void RecordConsumed(Creature creature, decimal amount)
    {
        if (amount <= 0) return;

        TotalConsumedByCreature[creature] =
            TotalConsumedByCreature.GetValueOrDefault(creature) + amount;

        var outstandingRevived = OutstandingRevivedByCreature.GetValueOrDefault(creature);
        var revivedConsumed = Math.Min(amount, outstandingRevived);
        OutstandingRevivedByCreature[creature] = outstandingRevived - revivedConsumed;

        var originalConsumed = OriginalConsumedByCreature.GetValueOrDefault(creature);
        OriginalConsumedByCreature[creature] = originalConsumed + amount - revivedConsumed;
    }

    public static void RecordNaturalGenerated(Creature creature, decimal amount)
    {
        if (amount <= 0) return;
        NaturalGeneratedByCreature[creature] =
            NaturalGeneratedByCreature.GetValueOrDefault(creature) + amount;
    }

    public static decimal GetNaturalGenerated(Creature creature)
        => NaturalGeneratedByCreature.GetValueOrDefault(creature);

    public static decimal GetTotalConsumed(Creature creature)
        => TotalConsumedByCreature.GetValueOrDefault(creature);

    public static decimal PreviewRevivePool(Creature creature, decimal ratio)
    {
        if (ratio <= 0) return 0m;

        var originalConsumed = OriginalConsumedByCreature.GetValueOrDefault(creature);
        var totalRevived = TotalRevivedByCreature.GetValueOrDefault(creature);
        var eligible = Math.Ceiling(originalConsumed * ratio) - totalRevived;
        if (eligible <= 0) return 0m;

        var currentPool = creature.Powers?.OfType<HeartCandlePower>().FirstOrDefault()?.Amount ?? 0m;
        var remainingCapacity = Math.Max(creature.CurrentHp - currentPool, 0m);
        return Math.Min(eligible, remainingCapacity);
    }

    public static void RecordRevived(Creature creature, decimal amount)
    {
        if (amount <= 0) return;
        OutstandingRevivedByCreature[creature] =
            OutstandingRevivedByCreature.GetValueOrDefault(creature) + amount;
        TotalRevivedByCreature[creature] =
            TotalRevivedByCreature.GetValueOrDefault(creature) + amount;
    }

    public static void CleanupDeadEntries()
    {
        foreach (var creature in OriginalConsumedByCreature.Keys.ToList())
        {
            if (creature.CombatState == null || creature.CurrentHp <= 0)
            {
                OriginalConsumedByCreature.Remove(creature);
                OutstandingRevivedByCreature.Remove(creature);
                TotalRevivedByCreature.Remove(creature);
                NaturalGeneratedByCreature.Remove(creature);
                TotalConsumedByCreature.Remove(creature);
            }
        }
    }

    public static void Clear()
    {
        OriginalConsumedByCreature.Clear();
        OutstandingRevivedByCreature.Clear();
        TotalRevivedByCreature.Clear();
        NaturalGeneratedByCreature.Clear();
        TotalConsumedByCreature.Clear();
    }
}

public static class EntelechiaDamage
{
    public static bool IsAttackDamage(ValueProp props, CardModel? cardSource)
    {
        return cardSource?.Type == CardType.Attack
            && (props == DamageProps.card || props == DamageProps.cardUnpowered);
    }
}

[HarmonyPatch]
public static class EntelechiaAttackCommandPatch
{
    public static MethodBase TargetMethod()
        => AccessTools.Method(typeof(AttackCommand), nameof(AttackCommand.Execute), [typeof(PlayerChoiceContext)]);

    public static async Task<AttackCommand> Postfix(Task<AttackCommand> __result, PlayerChoiceContext choiceContext)
    {
        var attack = await __result;
        try
        {
            foreach (var feast in attack.Attacker?.Powers?.OfType<BloodFeastPower>().ToList() ?? [])
                await feast.ReconcileCompletedAttack(choiceContext, attack);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[BloodFeast-Reconcile] {ex.Message}");
        }
        return attack;
    }
}

[HarmonyPatch]
public static class EntelechiaCardCastAnimationPatch
{
    public static MethodBase TargetMethod()
        => AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.OnPlayWrapper),
            [
                typeof(PlayerChoiceContext),
                typeof(Creature),
                typeof(bool),
                typeof(ResourceInfo),
                typeof(bool)
            ]);

    public static void Prefix(CardModel __instance)
    {
        if (__instance is EntelechiaCard card)
            card.ResetBlockAnimationRequest();
    }

    public static async Task Postfix(Task __result, CardModel __instance)
    {
        await __result;
        if (__instance is EntelechiaCard card)
            card.TryPlayCastAnimationAfterSuccessfulPlay();
    }
}

[HarmonyPatch]
public static class EntelechiaCardCanPlayPatch
{
    public static MethodBase TargetMethod()
        => AccessTools.Method(typeof(CardModel), nameof(CardModel.CanPlay), Type.EmptyTypes);

    public static void Postfix(CardModel __instance, ref bool __result)
    {
        if (__result && __instance is EntelechiaCard card && !card.CanPayRequiredHpCost())
            __result = false;
    }
}

[HarmonyPatch]
public static class EntelechiaCardCanPlayReasonPatch
{
    public static MethodBase TargetMethod()
        => AccessTools.Method(
            typeof(CardModel),
            nameof(CardModel.CanPlay),
            [typeof(UnplayableReason).MakeByRefType(), typeof(AbstractModel).MakeByRefType()]);

    public static void Postfix(CardModel __instance, ref bool __result, ref UnplayableReason reason, ref AbstractModel? preventer)
    {
        if (!__result || __instance is not EntelechiaCard card || card.CanPayRequiredHpCost()) return;
        __result = false;
        reason = UnplayableReason.BlockedByCardLogic;
        preventer = card;
    }
}

[HarmonyPatch]
public static class CombatPatches
{
    [HarmonyPatch(typeof(Creature), "AfterTurnStart")]
    [HarmonyPostfix]
    public static void AfterTurnStart_Postfix(Creature __instance)
    {
        try
        {
            if (!TurnStateTracker.IsPlayerCreature(__instance)) return;
            TurnStateTracker.Reset();
            BloodHarvestPower.ResetCardPlayHealLedger();
            HeartCandleLedger.CleanupDeadEntries();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[TurnTracker-Reset] {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(CombatManager), "SetUpCombat")]
    [HarmonyPostfix]
    public static void SetUpCombat_Postfix()
    {
        try
        {
            HeartCandleLedger.Clear();
            BloodHarvestPower.ResetCardPlayHealLedger();
            TurnStateTracker.Reset();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[CombatLedger-Reset] {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterBlockGained))]
    [HarmonyPostfix]
    public static async Task AfterBlockGained_Animation_Postfix(
        Task __result,
        Creature creature,
        decimal amount,
        CardModel? cardSource)
    {
        await __result;
        try
        {
            if (amount <= 0 || !TurnStateTracker.IsPlayerCreature(creature)) return;
            if (cardSource is EntelechiaCard card)
                card.RequestBlockAnimationAfterSuccessfulGain();
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[AnimationDriver] AfterBlockGained skipped: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterDamageReceived))]
    [HarmonyPostfix]
    public static async Task AfterDamageReceived_Animation_Postfix(
        Task __result,
        Creature target,
        DamageResult result,
        ValueProp props)
    {
        await __result;
        try
        {
            if (!TurnStateTracker.IsPlayerCreature(target) || result.UnblockedDamage <= 0) return;
            if (props == DamageProps.cardHpLoss || props == DamageProps.nonCardHpLoss) return;

            TryPlayCreatureAnimation(target, "PlayHurt", driver => driver.PlayHurt());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[AnimationDriver] AfterDamageReceived skipped: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCurrentHpChanged))]
    [HarmonyPostfix]
    public static async Task AfterCurrentHpChanged_Animation_Postfix(
        Task __result,
        Creature creature,
        decimal delta)
    {
        await __result;
        try
        {
            if (!TurnStateTracker.IsPlayerCreature(creature) || delta == 0) return;

            if (delta < 0)
            {
                TurnStateTracker.LostHpThisTurn = true;
                return;
            }

            TurnStateTracker.HealedThisTurn = true;
            TryPlayCreatureAnimation(creature, "PlayHeal", driver => driver.PlayHeal());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[AnimationDriver] AfterCurrentHpChanged skipped: {ex.Message}");
        }
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterDeath))]
    [HarmonyPostfix]
    public static async Task AfterDeath_Animation_Postfix(
        Task __result,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength)
    {
        var visualWait = Task.CompletedTask;
        try
        {
            if (!wasRemovalPrevented && TurnStateTracker.IsPlayerCreature(creature))
            {
                if (TryPlayCreatureAnimation(creature, "PlayDeath", driver => driver.PlayDeath()))
                {
                    visualWait = Cmd.Wait(
                        MathF.Max(deathAnimLength, EntelechiaCombatAnimationDriver.DeathDuration),
                        ignoreCombatEnd: true);
                }
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[AnimationDriver] AfterDeath skipped: {ex.Message}");
        }

        await Task.WhenAll(__result, visualWait);
    }

    [HarmonyPatch(typeof(Hook), nameof(Hook.AfterCombatEnd))]
    [HarmonyPostfix]
    public static async Task AfterCombatEnd_Animation_Postfix(
        Task __result,
        ICombatState combatState)
    {
        try
        {
            foreach (var creature in combatState.PlayerCreatures)
                TryPlayCreatureAnimation(creature, "CleanupForCombatEnd", driver => driver.CleanupForCombatEnd());
        }
        catch (Exception ex)
        {
            MainFile.Logger.Info($"[AnimationDriver] AfterCombatEnd skipped: {ex.Message}");
        }

        await __result;
    }

    private static bool TryPlayCreatureAnimation(
        Creature creature,
        string eventName,
        Action<EntelechiaCombatAnimationDriver> play)
    {
        try
        {
            var visuals = creature.GetCreatureNode()?.Visuals;
            if (visuals == null) return false;

            if (!EntelechiaCombatAnimationDriver.TryGet(visuals, out var driver) || driver == null)
                return false;

            play(driver);
            return true;
        }
        catch (Exception exception)
        {
            MainFile.Logger.Info(
                $"[AnimationDriver] {eventName} skipped: {exception.GetType().Name}: {exception.Message}");
            return false;
        }
    }
}
