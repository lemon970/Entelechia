using BaseLib.Abstracts;
using BaseLib.Utils.NodeFactories;
using Entelechia.EntelechiaCode.Animation;
using Entelechia.EntelechiaCode.Cards;
using Entelechia.EntelechiaCode.Extensions;
using Entelechia.EntelechiaCode.Relics;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.Combat;
using System.Text;

namespace Entelechia.EntelechiaCode.Character;

public class Entelechia : PlaceholderCharacterModel
{
    public const string CharacterId = "Entelechia";

    public static readonly Color Color = new("c94060");

    public Entelechia()
    {
        MainFile.Logger.Info($"Entelechia character model instantiated, Id={Id}");
    }

    public override Color NameColor => Color;
    public override CharacterGender Gender => CharacterGender.Feminine;
    public override int StartingHp => 50;

    // Force character to appear in vanilla char select (PlaceholderCharacterModel may default to hidden)
    public override bool HideFromVanillaCharacterSelect => false;

    // ponytail: GetOrCreate avoids DuplicateModelException when ScriptManagerBridge pre-registers cards
    private static T GetOrCreate<T>() where T : CardModel, new()
    {
        var existing = ModelDb.Card<T>();
        if (existing != null) return existing;
        MainFile.Logger.Info($"[StartingDeck] ModelDb missing {typeof(T).Name}, creating");
        return new T();
    }

    private static IEnumerable<CardModel>? _startingDeck;
    public override IEnumerable<CardModel> StartingDeck => _startingDeck ??= [
        GetOrCreate<Cards.BloodBlade>(),
        GetOrCreate<Cards.BloodBlade>(),
        GetOrCreate<Cards.BloodBlade>(),
        GetOrCreate<Cards.BloodBlade>(),
        GetOrCreate<Cards.BloodVeil>(),
        GetOrCreate<Cards.BloodVeil>(),
        GetOrCreate<Cards.BloodVeil>(),
        GetOrCreate<Cards.BloodVeil>(),
        GetOrCreate<Cards.RoseTrail>(),
        GetOrCreate<Cards.DiscontinuousPulse>()
    ];

    public override IReadOnlyList<RelicModel> StartingRelics =>
    [
        GetOrCreateRelic<Relics.BloodDemonReplete>()
    ];

    private static T GetOrCreateRelic<T>() where T : RelicModel, new()
    {
        return ModelDb.Relic<T>() ?? new T();
    }

    public override CardPoolModel CardPool => ModelDb.CardPool<EntelechiaCardPool>();
    public override RelicPoolModel RelicPool => ModelDb.RelicPool<EntelechiaRelicPool>();
    public override PotionPoolModel PotionPool => ModelDb.PotionPool<EntelechiaPotionPool>();

    private const string VisualsDiagnosticBuild = "20260710-statefx-death-cleanup-phase6";

    // CharacterSelectTransitionPath is not virtual; patched in MainFile instead
    public override NCreatureVisuals CreateCustomVisuals()
    {
        var path = $"{MainFile.ResPath}/images/character/idle.png";
        MainFile.Logger.Info($"[CreateCustomVisuals] build={VisualsDiagnosticBuild} path={path}");

        var visuals = CreateManualCreatureVisuals(path);
        if (visuals == null)
        {
            MainFile.Logger.Info("[CreateCustomVisuals] manual visuals returned null — falling back to base");
            return base.CreateCustomVisuals();
        }

        LogVisualDiagnostics(visuals);

        MainFile.Logger.Info("[CreateCustomVisuals] success");
        return visuals;
    }

    private static NCreatureVisuals? CreateManualCreatureVisuals(string path)
    {
        var texture = ResourceLoader.Load<Texture2D>(path);
        if (texture == null)
        {
            MainFile.Logger.Info($"[CreateCustomVisuals] failed to load texture: {path}");
            return null;
        }

        const float spriteScale = 0.48f;
        const float spriteOffsetX = 58.5f;
        const float boundsOffsetX = 20f;
        const float contentMinX = 174f;
        const float contentMinY = 361f;
        const float contentMaxX = 821f;
        const float contentMaxY = 1177f;
        const float bodyMinX = 368f;
        const float bodyMinY = 562f;
        const float bodyMaxX = 794f;
        const float bodyMaxY = 1177f;
        const float boundsPadX = 12f;
        const float boundsTopPadY = 12f;

        var textureSize = texture.GetSize();
        var textureCenter = textureSize * 0.5f;
        var contentCenter = new Vector2((contentMinX + contentMaxX) * 0.5f, (contentMinY + contentMaxY) * 0.5f);
        var contentSize = new Vector2(contentMaxX - contentMinX + 1f, contentMaxY - contentMinY + 1f) * spriteScale;
        var spritePosition = new Vector2(
            -(contentCenter.X - textureCenter.X) * spriteScale + spriteOffsetX,
            -(contentMaxY - textureCenter.Y) * spriteScale);
        var boundsAnchorPosition = new Vector2(
            -(contentCenter.X - textureCenter.X) * spriteScale + boundsOffsetX,
            spritePosition.Y);
        var contentTopLeft = new Vector2(
            (contentMinX - textureCenter.X) * spriteScale + spritePosition.X,
            (contentMinY - textureCenter.Y) * spriteScale + spritePosition.Y);
        var bodySize = new Vector2(bodyMaxX - bodyMinX + 1f, bodyMaxY - bodyMinY + 1f) * spriteScale;
        var bodyTopLeft = new Vector2(
            (bodyMinX - textureCenter.X) * spriteScale + boundsAnchorPosition.X,
            (bodyMinY - textureCenter.Y) * spriteScale + spritePosition.Y);


        var visuals = new NCreatureVisuals
        {
            Name = "EntelechiaVisuals",
            Scale = Vector2.One,
            DefaultScale = 1f
        };

        var bounds = new Control
        {
            Name = "Bounds",
            UniqueNameInOwner = true,
            Position = new Vector2(bodyTopLeft.X - boundsPadX, bodyTopLeft.Y - boundsTopPadY),
            Size = new Vector2(bodySize.X + boundsPadX * 2f, bodySize.Y + boundsTopPadY)
        };
        AddUniqueChild(visuals, bounds);

        var sprite = new Sprite2D
        {
            Name = "Visuals",
            UniqueNameInOwner = true,
            Texture = texture,
            Centered = true,
            Position = spritePosition,
            Scale = Vector2.One * spriteScale
        };
        AddUniqueChild(visuals, sprite);

        var intentPos = new Marker2D
        {
            Name = "IntentPos",
            UniqueNameInOwner = true,
            Position = bounds.Position + (bounds.Size * new Vector2(0.5f, 0f)) + new Vector2(0f, -70f)
        };
        AddUniqueChild(visuals, intentPos);

        var centerPos = new Marker2D
        {
            Name = "CenterPos",
            UniqueNameInOwner = true,
            Position = bounds.Position + (bounds.Size * new Vector2(0.5f, 0.6f))
        };
        AddUniqueChild(visuals, centerPos);

        var vfxRoot = new Node2D
        {
            Name = "VfxRoot",
            UniqueNameInOwner = true,
            Position = Vector2.Zero,
            Scale = Vector2.One,
            Rotation = 0f
        };
        AddUniqueChild(visuals, vfxRoot);

        var slashArc = CreateHiddenVfxSprite($"{MainFile.ResPath}/images/vfx/slash_arc.png", "SlashArc");
        AddUniqueChild(vfxRoot, slashArc, visuals);

        var impactBurst = CreateHiddenVfxSprite($"{MainFile.ResPath}/images/vfx/impact_burst.png", "ImpactBurst");
        AddUniqueChild(vfxRoot, impactBurst, visuals);

        var bloodDrops = CreateHiddenVfxSprite($"{MainFile.ResPath}/images/vfx/blood_drops.png", "BloodDrops");
        AddUniqueChild(vfxRoot, bloodDrops, visuals);

        var animationDriver = new EntelechiaCombatAnimationDriver(sprite, slashArc, impactBurst, bloodDrops);
        var animationDriverNode = new Node
        {
            Name = EntelechiaCombatAnimationDriver.NodeName,
            UniqueNameInOwner = true
        };
        AddUniqueChild(visuals, animationDriverNode);
        EntelechiaCombatAnimationDriver.Register(visuals, animationDriver);
        EntelechiaCombatAnimationDriver.Register(animationDriverNode, animationDriver);
        animationDriver.ResetVisuals();
        animationDriverNode.TreeEntered += animationDriver.PlayIdle;
        animationDriverNode.TreeExiting += animationDriver.Dispose;
        MainFile.Logger.Info($"[CreateCustomVisuals] animation driver ready: {animationDriver.DescribeState()}");

        MainFile.Logger.Info(
            $"[CreateCustomVisuals] computed trim: texture={textureSize}, contentBounds=({contentMinX},{contentMinY})..({contentMaxX},{contentMaxY}), " +
            $"contentSize={contentSize}, contentTopLeft={contentTopLeft}, bodyBounds=({bodyMinX},{bodyMinY})..({bodyMaxX},{bodyMaxY}), " +
            $"bodySize={bodySize}, spritePosition={spritePosition}, boundsPos={bounds.Position}, boundsSize={bounds.Size}, " +
            $"centerPos={centerPos.Position}, intentPos={intentPos.Position}");

        return visuals;
    }

    private static Sprite2D CreateHiddenVfxSprite(string path, string name)
    {
        var texture = ResourceLoader.Load<Texture2D>(path);
        if (texture == null)
        {
            MainFile.Logger.Info($"[CreateCustomVisuals] failed to load VFX texture for {name}: {path}");
        }

        return new Sprite2D
        {
            Name = name,
            UniqueNameInOwner = true,
            Texture = texture,
            Centered = true,
            Visible = false,
            Modulate = new Color(1f, 1f, 1f, 0f),
            Scale = Vector2.One,
            Rotation = 0f
        };
    }

    private static void AddUniqueChild(Node root, Node child, Node? owner = null)
    {
        child.UniqueNameInOwner = true;
        root.AddChild(child);
        child.Owner = owner ?? root;
    }

    private static void LogVisualDiagnostics(NCreatureVisuals visuals)
    {
        MainFile.Logger.Info(
            $"[CreateCustomVisuals] root: {DescribeNode(visuals)}, defaultScale={visuals.DefaultScale}, childCount={visuals.GetChildCount()}");

        var body = visuals.GetNodeOrNull<Node>("%Visuals");
        var bounds = visuals.GetNodeOrNull<Node>("%Bounds");
        var centerPos = visuals.GetNodeOrNull<Node>("%CenterPos");
        var intentPos = visuals.GetNodeOrNull<Node>("%IntentPos");
        var vfxRoot = visuals.GetNodeOrNull<Node>("%VfxRoot");
        var slashArc = visuals.GetNodeOrNull<Node>("%SlashArc");
        var impactBurst = visuals.GetNodeOrNull<Node>("%ImpactBurst");
        var bloodDrops = visuals.GetNodeOrNull<Node>("%BloodDrops");
        var animationDriver = visuals.GetNodeOrNull<Node>($"%{EntelechiaCombatAnimationDriver.NodeName}");

        MainFile.Logger.Info($"[CreateCustomVisuals] %Visuals: {DescribeNode(body)}");
        MainFile.Logger.Info($"[CreateCustomVisuals] %Bounds: {DescribeNode(bounds)}");
        MainFile.Logger.Info($"[CreateCustomVisuals] %CenterPos: {DescribeNode(centerPos)}");
        MainFile.Logger.Info($"[CreateCustomVisuals] %IntentPos: {DescribeNode(intentPos)}");
        MainFile.Logger.Info($"[CreateCustomVisuals] %VfxRoot: {DescribeNode(vfxRoot)}");
        MainFile.Logger.Info($"[CreateCustomVisuals] %SlashArc: {DescribeNode(slashArc)}");
        MainFile.Logger.Info($"[CreateCustomVisuals] %ImpactBurst: {DescribeNode(impactBurst)}");
        MainFile.Logger.Info($"[CreateCustomVisuals] %BloodDrops: {DescribeNode(bloodDrops)}");
        MainFile.Logger.Info($"[CreateCustomVisuals] %{EntelechiaCombatAnimationDriver.NodeName}: {DescribeNode(animationDriver)}");
        MainFile.Logger.Info("[CreateCustomVisuals] node tree:");
        LogNodeTree(visuals);
    }

    private static void LogNodeTree(Node node, int depth = 0, int maxDepth = 2)
    {
        MainFile.Logger.Info($"[CreateCustomVisuals] tree {new string(' ', depth * 2)}- {DescribeNode(node)}");

        if (depth >= maxDepth) return;

        for (var i = 0; i < node.GetChildCount(); i++)
        {
            LogNodeTree(node.GetChild(i), depth + 1, maxDepth);
        }
    }

    private static string DescribeNode(Node? node)
    {
        if (node == null) return "missing";

        var details = new StringBuilder($"{node.Name}:{node.GetType().Name}");

        if (node is CanvasItem canvasItem)
        {
            details.Append($", visible={canvasItem.Visible}, modulate={canvasItem.Modulate}, zIndex={canvasItem.ZIndex}");
        }

        if (node is Control control)
        {
            details.Append($", pos={control.Position}, globalPos={control.GlobalPosition}, size={control.Size}, scale={control.Scale}");
        }

        if (node is Node2D node2D)
        {
            details.Append($", pos={node2D.Position}, globalPos={node2D.GlobalPosition}, scale={node2D.Scale}, rotation={node2D.Rotation}");
        }

        if (node is Sprite2D sprite2D)
        {
            var texSize = sprite2D.Texture?.GetSize() ?? Vector2.Zero;
            details.Append(
                $", texture={texSize}, centered={sprite2D.Centered}, offset={sprite2D.Offset}, region={sprite2D.RegionEnabled}");
        }

        if (node is CollisionShape2D collisionShape)
        {
            details.Append($", shape={collisionShape.Shape?.GetType().Name ?? "null"}");
        }

        return details.ToString();
    }

    public override Control CustomIcon
    {
        get
        {
            var icon = NodeFactory<Control>.CreateFromResource(CustomIconTexturePath);
            icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            return icon;
        }
    }
    public override string CustomIconTexturePath => "character_icon_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectBg =>
        $"{MainFile.ResPath}/scenes/screens/char_select/char_select_bg_entelechia.tscn";
    public override string CustomMerchantAnimPath =>
        $"{MainFile.ResPath}/scenes/merchant/characters/entelechia_merchant.tscn";
    public override string CustomRestSiteAnimPath =>
        $"{MainFile.ResPath}/scenes/rest_site/characters/entelechia_rest_site.tscn";
    public override string CustomArmPointingTexturePath =>
        $"{MainFile.ResPath}/images/ui/hands/multiplayer_hand_entelechia_point.png";
    public override string CustomArmRockTexturePath =>
        $"{MainFile.ResPath}/images/ui/hands/multiplayer_hand_entelechia_rock.png";
    public override string CustomArmScissorsTexturePath =>
        $"{MainFile.ResPath}/images/ui/hands/multiplayer_hand_entelechia_scissors.png";
    public override string CustomArmPaperTexturePath =>
        $"{MainFile.ResPath}/images/ui/hands/multiplayer_hand_entelechia_paper.png";
    public override string CustomCharacterSelectIconPath => "char_select_char_name.png".CharacterUiPath();
    public override string CustomCharacterSelectLockedIconPath => "char_select_char_name_locked.png".CharacterUiPath();
    public override string CustomMapMarkerPath => "map_marker_char_name.png".CharacterUiPath();
}
