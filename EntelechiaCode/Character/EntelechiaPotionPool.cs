using BaseLib.Abstracts;
using Entelechia.EntelechiaCode.Extensions;
using Godot;

namespace Entelechia.EntelechiaCode.Character;

public class EntelechiaPotionPool : CustomPotionPoolModel
{
    public override Color LabOutlineColor => Entelechia.Color;
    

    public override string BigEnergyIconPath => "charui/big_energy.png".ImagePath();
    public override string TextEnergyIconPath => "charui/text_energy.png".ImagePath();
}