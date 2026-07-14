using MegaCrit.Sts2.Core.Entities.Powers;

namespace Entelechia.EntelechiaCode.Powers;

public class EternalRepletePower : EntelechiaPower
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Single;

    public decimal HealRatio { get; set; } = 0.50m;
    public decimal EmberHarvestAmount { get; set; } = 2m;
}
