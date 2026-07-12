using MegaCrit.Sts2.Core.Entities.Powers;

namespace Entelechia.EntelechiaCode.Powers;

public class EternalRepletePower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public decimal HealRatio => HealRatioFor(Amount);

    public static decimal HealRatioFor(decimal amount) => amount > 1 ? 0.55m : 0.50m;
}
