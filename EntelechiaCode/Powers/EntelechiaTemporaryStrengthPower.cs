using BaseLib.Abstracts;
using BaseLib.Extensions;
using Entelechia.EntelechiaCode.Extensions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace Entelechia.EntelechiaCode.Powers;

public abstract class EntelechiaTemporaryStrengthPower<TOrigin> : TemporaryStrengthPower, ICustomPower
    where TOrigin : CardModel
{
    public override CardModel OriginModel => ModelDb.Card<TOrigin>();

    public override LocString Title => new("powers", $"{Id.Entry}.title");
    public override LocString Description => new("powers", $"{Id.Entry}.description");
    protected override string SmartDescriptionLocKey => $"{Id.Entry}.smartDescription";

    public string CustomPackedIconPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".PowerImagePath();

    public string CustomBigIconPath =>
        $"{Id.Entry.RemovePrefix().ToLowerInvariant()}.png".BigPowerImagePath();
}
