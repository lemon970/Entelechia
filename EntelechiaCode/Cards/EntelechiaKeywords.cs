using BaseLib.Patches.Compatibility;
using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace Entelechia.EntelechiaCode.Cards;

public static class EntelechiaKeywords
{
    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword HighHealth;

    [CustomEnum]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword LowHealth;
}
