using System.Reflection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Entelechia.EntelechiaCode;

internal static class Sts2Compatibility
{
    private static readonly MethodInfo CreatureDamageMethod = ResolveCreatureDamageMethod();

    public static Task Damage(
        PlayerChoiceContext context,
        Creature target,
        decimal amount,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource,
        CardPlay? cardPlay)
    {
        var parameters = CreatureDamageMethod.GetParameters().Length == 7
            ? new object?[] { context, target, amount, props, dealer, cardSource, cardPlay }
            : new object?[] { context, target, amount, props, dealer, cardSource };

        return (Task)(CreatureDamageMethod.Invoke(null, parameters)
            ?? throw new InvalidOperationException("CreatureCmd.Damage returned null."));
    }

    private static MethodInfo ResolveCreatureDamageMethod()
    {
        var candidates = typeof(CreatureCmd).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(method => method.Name == nameof(CreatureCmd.Damage))
            .Where(method =>
            {
                var parameters = method.GetParameters();
                return parameters.Length is 6 or 7
                    && parameters[0].ParameterType == typeof(PlayerChoiceContext)
                    && parameters[1].ParameterType == typeof(Creature)
                    && parameters[2].ParameterType == typeof(decimal)
                    && parameters[3].ParameterType == typeof(ValueProp)
                    && parameters[4].ParameterType == typeof(Creature)
                    && parameters[5].ParameterType == typeof(CardModel)
                    && (parameters.Length == 6 || parameters[6].ParameterType == typeof(CardPlay));
            })
            .OrderByDescending(method => method.GetParameters().Length)
            .ToList();

        return candidates.FirstOrDefault()
            ?? throw new MissingMethodException(
                typeof(CreatureCmd).FullName,
                "Damage(PlayerChoiceContext, Creature, decimal, ValueProp, Creature, CardModel[, CardPlay])");
    }
}
