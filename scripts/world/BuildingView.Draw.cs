using Godot;
using ProceduralRts.Core;

namespace ProceduralRts.World;

public partial class BuildingView : Node2D
{
    private void DrawFootprint(Rect2 rect, Color accent, BuildingArtColors art)
    {
        var pad = 12;
        var footprint = rect.Grow(pad);
        DrawRect(footprint, new Color(art.Effect, 0.045f), true);
        DrawRect(footprint, new Color(art.Ink, 0.54f), false, 5.2f);
        DrawRect(footprint, new Color(art.Effect, 0.26f), false, 1.4f);

        const float corner = 24;
        DrawLine(footprint.Position, footprint.Position + new Vector2(corner, 0), new Color(art.Effect, 0.7f), 2.6f, true);
        DrawLine(footprint.Position, footprint.Position + new Vector2(0, corner), new Color(art.Effect, 0.7f), 2.6f, true);
        DrawLine(new Vector2(footprint.End.X, footprint.Position.Y), new Vector2(footprint.End.X - corner, footprint.Position.Y), new Color(art.Effect, 0.7f), 2.6f, true);
        DrawLine(new Vector2(footprint.End.X, footprint.Position.Y), new Vector2(footprint.End.X, footprint.Position.Y + corner), new Color(art.Effect, 0.7f), 2.6f, true);
        DrawLine(new Vector2(footprint.Position.X, footprint.End.Y), new Vector2(footprint.Position.X + corner, footprint.End.Y), new Color(art.Effect, 0.7f), 2.6f, true);
        DrawLine(new Vector2(footprint.Position.X, footprint.End.Y), new Vector2(footprint.Position.X, footprint.End.Y - corner), new Color(art.Effect, 0.7f), 2.6f, true);
        DrawLine(footprint.End, footprint.End - new Vector2(corner, 0), new Color(art.Effect, 0.7f), 2.6f, true);
        DrawLine(footprint.End, footprint.End - new Vector2(0, corner), new Color(art.Effect, 0.7f), 2.6f, true);
    }

    private void DrawStructure(
        Rect2 rect,
        Color accent,
        BuildingArtColors art,
        float pulse,
        bool powered,
        float buildProgress,
        bool constructionPaused,
        ConstructionPauseReason pauseReason,
        string kind)
    {
        DrawRect(rect, new Color(art.Shadow, 0.14f), true);
        DrawRect(rect.Grow(-4), new Color(art.Body, 0.78f), true);
        DrawRect(rect, new Color(art.Effect, 0.82f), false, 2.4f);
        DrawRect(rect.Grow(-6), new Color(art.Highlight, 0.22f), false, 1.0f);

        switch (kind)
        {
            case BuildingDesignIds.Headquarters:
                DrawHeadquarters(rect, accent, art, pulse);
                break;
            case BuildingDesignIds.PowerPlant:
                DrawPowerPlant(rect, accent, art, pulse);
                break;
            case BuildingDesignIds.Barracks:
                DrawBarracks(rect, accent, art);
                break;
            case BuildingDesignIds.VehicleFactory:
                DrawVehicleFactory(rect, accent, art);
                break;
            case BuildingDesignIds.Refinery:
                DrawRefinery(rect, accent, art, pulse);
                break;
            case BuildingDesignIds.Airfield:
                DrawAirfield(rect, accent, art);
                break;
            case BuildingDesignIds.GroundTurret:
                DrawTurretPlatform(rect, art, pulse, antiAir: false);
                break;
            case BuildingDesignIds.AntiAirTurret:
                DrawTurretPlatform(rect, art, pulse, antiAir: true);
                break;
        }

        if (!powered)
        {
            DrawRect(rect.Grow(-8), new Color(art.Ink, 0.16f), true);
            DrawLine(rect.Position + new Vector2(16, 16), rect.End - new Vector2(16, 16), new Color(art.Ink, 0.46f), 2.0f, true);
            DrawLine(new Vector2(rect.End.X - 16, rect.Position.Y + 16), new Vector2(rect.Position.X + 16, rect.End.Y - 16), new Color(art.Ink, 0.32f), 1.4f, true);
        }

        if (buildProgress < 1)
        {
            var clamped = Mathf.Clamp(buildProgress, 0, 1);
            var scaffold = new Rect2(rect.Position, new Vector2(rect.Size.X * clamped, rect.Size.Y));
            DrawRect(scaffold.Grow(-10), new Color(art.Effect, 0.16f), true);
            DrawRect(rect.Grow(-10), new Color(art.Effect, 0.44f), false, 1.6f);
        }

        if (constructionPaused)
        {
            DrawPausedConstructionProgress(rect, art, buildProgress, pauseReason);
        }

        if (!powered || constructionPaused)
        {
            DrawOfflineStatusBadge(rect, art, pulse, powered, constructionPaused, pauseReason);
        }
    }
    private void DrawOwnershipZones(Rect2 rect, Color ownerColor, BuildingArtColors art)
    {
        var stripeY = rect.Position.Y + 10;
        var stripeWidth = Mathf.Min(48, rect.Size.X * 0.28f);
        DrawLine(new Vector2(rect.Position.X + 18, stripeY), new Vector2(rect.Position.X + 18 + stripeWidth, stripeY), new Color(ownerColor, 0.70f), 3.0f, true);
        DrawLine(new Vector2(rect.End.X - 18 - stripeWidth, stripeY), new Vector2(rect.End.X - 18, stripeY), new Color(ownerColor, 0.70f), 3.0f, true);
        DrawLine(new Vector2(rect.Position.X + 22, stripeY + 8), new Vector2(rect.Position.X + 22 + stripeWidth * 0.58f, stripeY + 8), new Color(ownerColor, 0.30f), 1.4f, true);
        DrawLine(new Vector2(rect.End.X - 22 - stripeWidth * 0.58f, stripeY + 8), new Vector2(rect.End.X - 22, stripeY + 8), new Color(ownerColor, 0.30f), 1.4f, true);

        var bannerSize = new Vector2(14, 8);
        var banners = new[]
        {
            new Rect2(rect.Position + new Vector2(12, 12), bannerSize),
            new Rect2(new Vector2(rect.End.X - 28, rect.Position.Y + 12), bannerSize),
            new Rect2(new Vector2(rect.Position.X + 12, rect.End.Y - 20), bannerSize),
            new Rect2(rect.End - new Vector2(26, 20), bannerSize),
        };

        foreach (var banner in banners)
        {
            DrawRect(banner, new Color(ownerColor, 0.58f), true);
            DrawRect(banner, new Color(art.Ink, 0.42f), false, 0.9f);
        }

        var rallyPoint = _buildingProjection?.RallyPoint ?? Building.RallyPoint;
        if (rallyPoint is null)
        {
            return;
        }

        var plaque = new Rect2(new Vector2(rect.End.X - 42, rect.End.Y - 34), new Vector2(26, 12));
        DrawRect(plaque, new Color(ownerColor, 0.42f), true);
        DrawRect(plaque, new Color(art.Highlight, 0.30f), false, 1.0f);
    }

    private void DrawHealth(Vector2 footprint, float maxHpFallback, Color accent)
    {
        var width = footprint.X * 0.72f;
        var y = -footprint.Y / 2f - 22;
        var hp = _projection?.Hp ?? Building.Hp;
        var maxHp = maxHpFallback;
        var health = maxHp <= 0 ? 0 : Mathf.Clamp(hp / maxHp, 0, 1);
        DrawRect(new Rect2(-width / 2, y, width, 5.5f), new Color(SoftOldCityPalette.Ink, 0.58f));
        DrawRect(new Rect2(-width / 2, y, width * health, 5.5f), new Color(accent, 0.86f));
    }

    private void DrawProduction(Vector2 footprint, Color accent, BuildingArtColors art)
    {
        var projectedQueue = _buildingProjection?.ProductionQueue;
        var queueCount = projectedQueue?.Count ?? Building.ProductionQueue.Count;
        if (queueCount == 0)
        {
            return;
        }

        var productionKind = projectedQueue is not null
            ? projectedQueue[0].Kind
            : Building.ProductionQueue[0].Kind;
        var designId = projectedQueue is not null
            ? projectedQueue[0].DesignId
            : Building.ProductionQueue[0].DesignId;
        var itemProgress = projectedQueue is not null
            ? projectedQueue[0].Progress
            : Building.ProductionQueue[0].Progress;
        var production = UnitDesignCatalog.Spec(designId).Production
            ?? throw new InvalidOperationException($"UnitDesign '{designId}' must include ProductionSpec for {productionKind}.");
        var width = footprint.X * 0.72f;
        var y = -footprint.Y / 2f - 12;
        var progress = Mathf.Clamp(itemProgress / production.Duration, 0, 1);
        DrawRect(new Rect2(-width / 2, y, width, 4.5f), new Color(art.Ink, 0.52f));
        DrawRect(new Rect2(-width / 2, y, width * progress, 4.5f), new Color(art.Highlight, 0.66f));
        DrawLine(new Vector2(-width / 2, y + 8), new Vector2(-width / 2 + Mathf.Min(queueCount, 5) * 12, y + 8), new Color(art.Effect, 0.82f), 2.2f, true);
    }

    private void DrawSelection(Vector2 footprint, Color accent, float pulse)
    {
        var selected = _projection?.Selected ?? Building.Selected;
        if (!selected)
        {
            return;
        }

        var rallyPulse = _buildingProjection?.RallyPulse ?? Building.RallyPulse;
        var rect = new Rect2(-footprint / 2f, footprint).Grow(19 + rallyPulse * 6);
        DrawRect(rect, new Color(SoftOldCityPalette.InnerLight, 0.78f), false, 2.2f);
        DrawRect(rect.Grow(8 + pulse * 3), new Color(accent, 0.38f + rallyPulse * 0.22f), false, 1.4f);
    }
}
