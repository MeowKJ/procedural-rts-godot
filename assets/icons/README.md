Icon asset policy

This project ships a small MIT-licensed Tabler Icons SVG subset for HUD controls, production cards, unit classes, and command modes.

Game-icons.net assets are intentionally not bundled. Their CC BY 3.0 attribution requirements are acceptable for some projects, but this prototype keeps the runtime asset chain simpler by using procedural glyph drawing for weapon, ammo, status, and special-attack semantics when Tabler does not provide an exact match.

Rules:
- Packaged SVG files should live under assets/icons/tabler and be covered by the local Tabler attribution and license files.
- Do not add assets/icons/game-icons unless the UI explicitly surfaces attribution and tests are updated for CC BY 3.0 compliance.
- Prefer procedural glyph fallback in HudLayer and world renderers for RTS-specific weapon/unit semantics.
