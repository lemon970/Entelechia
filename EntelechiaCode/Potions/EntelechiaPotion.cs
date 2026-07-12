using BaseLib.Abstracts;
using BaseLib.Utils;
using Entelechia.EntelechiaCode.Character;

namespace Entelechia.EntelechiaCode.Potions;

[Pool(typeof(EntelechiaPotionPool))]
public abstract class EntelechiaPotion : CustomPotionModel;