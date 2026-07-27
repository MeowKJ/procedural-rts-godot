static partial class Program
{
    private static void AssertPresentationDescriptorsAndLocalization()
    {
        var productionPresentationFactions = new[] { UnitFactionId.Dog, UnitFactionId.Cat };
        var factionProductionPresentations = productionPresentationFactions
            .SelectMany(faction => Enum.GetValues<ProductionKind>().Select(kind =>
            {
                var designId = UnitDesignRuntimeLoadouts.ProductionDesignId(faction, kind)
                    ?? throw new InvalidOperationException($"{faction} {kind} should resolve to a UnitSpec production design id");
                var spec = UnitDesignCatalog.Spec(designId);
                return (Faction: faction, Kind: kind, Spec: spec, Presentation: UnitPresentationCatalog.For(faction, kind));
            }))
            .ToArray();

        if (factionProductionPresentations.Length != productionPresentationFactions.Length * Enum.GetValues<ProductionKind>().Length)
        {
            throw new InvalidOperationException("every playable faction retired production kind should resolve to UnitSpec production presentation metadata");
        }

        var playableUnitSpecPresentations = UnitDesignCatalog.Designs.Values
            .Select(design => design.ToSpec())
            .Where(spec => spec.Faction is UnitFactionId.Dog or UnitFactionId.Cat)
            .OrderBy(spec => spec.Id)
            .Select(spec => (Spec: spec, Presentation: UnitPresentationCatalog.ForSpec(spec)))
            .ToArray();

        foreach (var (spec, descriptor) in playableUnitSpecPresentations)
        {
            if (descriptor.Icon == IconGlyph.None
                || !GameText.HasTranslation(descriptor.NameKey, GameLanguage.English)
                || !GameText.HasTranslation(descriptor.NameKey, GameLanguage.ChineseSimplified)
                || !GameText.HasTranslation(descriptor.RoleKey, GameLanguage.English)
                || !GameText.HasTranslation(descriptor.RoleKey, GameLanguage.ChineseSimplified)
                || descriptor.Art.Layers.Count == 0
                || descriptor.Art.StatusGlyph == IconGlyph.None
                || !descriptor.Art.PlayerColorZones.Any())
            {
                throw new InvalidOperationException($"UnitSpec presentation descriptor is incomplete for {spec.Id}");
            }

            if (!descriptor.Art.Layers.Any(layer => layer.ColorRole == ColorRole.Body)
                || !descriptor.Art.Layers.Any(layer => layer.ColorRole == ColorRole.Ink)
                || !descriptor.Art.Layers.Any(layer => layer.ColorRole == ColorRole.Effect))
            {
                throw new InvalidOperationException($"UnitSpec art should include reusable body, ink, and effect layers for {spec.Id}");
            }
        }

        foreach (var (faction, kind, spec, descriptor) in factionProductionPresentations)
        {
            if (descriptor.Icon == IconGlyph.None
                || !GameText.HasTranslation(descriptor.TooltipKey, GameLanguage.English)
                || !GameText.HasTranslation(descriptor.TooltipKey, GameLanguage.ChineseSimplified)
                || descriptor.OutputDesignId != spec.Id
                || descriptor.ShortCode != spec.ShortCode
                || descriptor.Icon != spec.Icon
                || descriptor.Category != spec.Production!.Category
                || descriptor.RoleGlyph == IconGlyph.None)
            {
                throw new InvalidOperationException($"{faction} {kind} production presentation descriptor should resolve UnitSpec output metadata");
            }
        }

        var designProductionPresentations = UnitDesignCatalog.Designs.Values
            .Select(design => design.ToSpec())
            .Where(spec => spec.Faction is UnitFactionId.Dog or UnitFactionId.Cat && spec.Production is not null)
            .OrderBy(spec => spec.Id)
            .Select(spec => (Spec: spec, Presentation: UnitPresentationCatalog.ForProductionSpec(ProductionKindDesignBridge.ProductionKindFor(spec), spec)))
            .ToArray();

        foreach (var (spec, descriptor) in designProductionPresentations)
        {
            if (descriptor.OutputDesignId != spec.Id
                || descriptor.ShortCode != spec.ShortCode
                || descriptor.Icon != spec.Icon
                || descriptor.Accent != SoftOldCityPalette.FactionColor(spec.Faction)
                || descriptor.RoleGlyph == IconGlyph.None
                || descriptor.Category != spec.Production!.Category)
            {
                throw new InvalidOperationException($"UnitSpec production presentation should project authored output metadata for {spec.Id}");
            }
        }

        var requiredGlyphs = playableUnitSpecPresentations.Select(entry => entry.Presentation.Icon)
            .Concat(factionProductionPresentations.Select(entry => entry.Presentation.Icon))
            .Concat(designProductionPresentations.Select(entry => entry.Presentation.Icon))
            .Concat([IconGlyph.Building, IconGlyph.Group, IconGlyph.Move, IconGlyph.AttackMove, IconGlyph.IgnoreMove, IconGlyph.Cancel, IconGlyph.Credits])
            .Where(glyph => glyph != IconGlyph.None)
            .Distinct()
            .ToList();
        foreach (var glyph in requiredGlyphs)
        {
            if (!IconLibrary.TryPath(glyph, out var resourcePath))
            {
                throw new InvalidOperationException($"icon library should map required glyph {glyph} to a Tabler SVG path");
            }

            var diskPath = resourcePath.Replace("res://", "");
            if (!File.Exists(diskPath))
            {
                throw new InvalidOperationException($"icon asset is missing for {glyph}: {resourcePath}");
            }
        }

        if (!File.Exists(IconLibrary.TablerAttributionPath.Replace("res://", ""))
            || !File.Exists("assets/icons/tabler/LICENSE.tabler-icons.txt"))
        {
            throw new InvalidOperationException("Tabler icon subset should include local attribution and MIT license files");
        }

        if (!File.Exists("assets/icons/README.md")
            || !File.ReadAllText("assets/icons/README.md").Contains("Game-icons.net assets are intentionally not bundled", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("icon policy should document that Game-icons.net semantics are replaced by procedural glyphs unless attribution is surfaced");
        }

        if (Directory.Exists("assets/icons/game-icons")
            || IconLibrary.Paths.Values.Any(path => !path.StartsWith("res://assets/icons/tabler/", StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("runtime icon library should avoid bundled Game-icons.net assets and use Tabler/procedural glyphs instead");
        }

        GameText.CurrentLanguage = GameLanguage.ChineseSimplified;
        if (GameText.T("unit.lightTank.name") != "\u77e2\u91cf\u5766\u514b"
            || GameText.T("ui.noSelection.title") != "\u672a\u9009\u62e9"
            || GameText.T("preview.attackStructure") != "\u653b\u51fb\u5efa\u7b51"
            || GameText.Format("preview.attack.matchup", GameText.Format("preview.matchup.good", GameText.T("preview.target.armor"))) != "\u653b\u51fb - \u514b\u5236 \u88c5\u7532"
            || GameText.T("menu.startSkirmish") != "\u5f00\u59cb\u906d\u9047\u6218"
            || GameText.T("pause.title") != "\u6218\u672f\u6682\u505c"
            || GameText.T("settings.title") != "\u8bbe\u7f6e"
            || GameText.T("settings.impactShake") != "\u547d\u4e2d\u9707\u52a8"
            || GameText.T("outcome.retry") != "\u91cd\u8bd5\u906d\u9047\u6218"
            || GameText.T("move.attack") != "\u653b\u51fb\u524d\u8fdb"
            || GameText.T("hotkeys.title") != "\u70ed\u952e")
        {
            throw new InvalidOperationException("localization should return readable zh-CN strings for known keys");
        }

        if (GameText.T("missing.localization.key") != "missing.localization.key")
        {
            throw new InvalidOperationException("localization should fallback unknown keys to their key name");
        }

        if (GameText.Keys.Any(key => !GameText.HasTranslation(key, GameLanguage.ChineseSimplified)))
        {
            throw new InvalidOperationException("every English localization key should have a zh-CN preload entry");
        }

        DisplayAudioSettings.ApplyLanguage(GameLanguage.ChineseSimplified, persist: false);
        if (DisplayAudioSettings.Language != GameLanguage.ChineseSimplified
            || GameText.CurrentLanguage != GameLanguage.ChineseSimplified
            || string.IsNullOrWhiteSpace(DisplayAudioSettings.LanguageLabel(GameLanguage.ChineseSimplified)))
        {
            throw new InvalidOperationException("settings should apply and expose the selected interface language");
        }

        DisplayAudioSettings.ApplyLanguage(GameLanguage.English, persist: false);
        if (DisplayAudioSettings.Language != GameLanguage.English
            || GameText.CurrentLanguage != GameLanguage.English)
        {
            throw new InvalidOperationException("settings should switch interface language back to English");
        }

        GameText.CurrentLanguage = GameLanguage.English;
    }
}
