static class ClassSilhouetteReviewGate
{
    public static void Check(string root, GateResult result)
    {
        var dogArt = ReviewGateSource.Read(root, "scripts", "core", "units", "dog", "DogUnitArt.cs");
        var footprint = ReviewGateSource.Read(root, "scripts", "core", "presentation", "vfx", "FootprintVisualMath.cs");
        var buildingDraw = ReviewGateSource.Read(root, "scripts", "world", "BuildingView.Draw.cs");
        var buildingGeometry = ReviewGateSource.Read(root, "scripts", "world", "BuildingView.Geometry.cs");

        RequireText(dogArt, "[\"light-step\", \"body-fixed-main\"]", "Light/infantry unit art must keep compact body-fixed light-step identity.", result);
        RequireText(dogArt, "[\"tracked-idle\", \"turret-follow-main\"]", "Vehicle art must keep tracked silhouette and a rotating turret mount.", result);
        RequireText(dogArt, "[\"air-patrol\", \"contrail-soft\", \"body-fixed-main\"]", "Aircraft art must keep floating/contrail silhouette hints.", result);
        RequireText(footprint, "MovementDomain.Naval => new FootprintStyle(\n                FootprintMarkKind.Wake", "Ship silhouette policy must stay paper-only with reserved wake ripples.", result);
        RequireText(buildingDraw, "DrawOwnershipZones(Rect2 rect, Color ownerColor, BuildingArtColors art)", "Buildings must keep owner-color banner/stripe zones separate from body fill.", result);
        RequireText(buildingDraw, "DrawRect(rect.Grow(-4), new Color(art.Body, 0.78f), true)", "Buildings must keep warm repaired-facility body fill.", result);
        RequireText(buildingDraw, "DrawRect(rect, new Color(art.Effect, 0.82f), false, 2.4f)", "Buildings must keep ink/effect outline readability.", result);
        RequireText(buildingDraw, "var banners = new[]", "Buildings must keep corner banner owner-color markers.", result);
        RequireText(buildingDraw, "const float corner = 24", "Building footprints must keep readable corner marks.", result);
        RequireText(buildingGeometry, "DrawTurretPlatform(Rect2 rect, BuildingArtColors art, float pulse, bool antiAir)", "Turrets must use a compact fixed-weapon platform renderer.", result);
        RequireText(buildingGeometry, "DrawSetTransform(Vector2.Zero, _buildingProjection!.Value.TurretFacing - bodyFacing, Vector2.One)", "Turret mounts must rotate independently from the platform body.", result);
        RequireText(buildingGeometry, "DrawLine(new Vector2(4, 0), new Vector2(radius + 22, 0)", "Ground turret must keep a prominent barrel silhouette.", result);
    }
}
