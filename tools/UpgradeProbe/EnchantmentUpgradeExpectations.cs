using Entelechia.EntelechiaCode.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;

internal static class EnchantmentUpgradeExpectations
{
    internal static void Run(List<string> failures)
    {
        CheckSharpDamage("upgrade then enchant", upgradeFirst: true, failures);
        CheckSharpDamage("enchant then upgrade", upgradeFirst: false, failures);
    }

    private static void CheckSharpDamage(string order, bool upgradeFirst, List<string> failures)
    {
        var card = new BloodStrike();
        var enchantment = new Sharp();
        UpgradeTestHarness.MakeMutableForTest(card);
        UpgradeTestHarness.MakeMutableForTest(enchantment);

        if (upgradeFirst)
            Upgrade(card);

        card.EnchantInternal(enchantment, 2m);
        enchantment.ModifyCard();

        if (!upgradeFirst)
            Upgrade(card);

        UpgradeTestHarness.CheckValue(
            nameof(BloodStrike),
            $"Sharp damage after {order}",
            card.DynamicVars.Damage.EnchantedValue,
            16m,
            failures);
    }

    private static void Upgrade(CardModel card)
    {
        card.UpgradeInternal();
        card.FinalizeUpgradeInternal();
    }

}
