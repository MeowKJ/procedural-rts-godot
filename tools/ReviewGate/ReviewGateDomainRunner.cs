static class ReviewGateDomainRunner
{
    public static void Run(string mode, ReviewGateContext context)
    {
        if (mode.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            RunAll(context);
            return;
        }

        switch (mode)
        {
            case "backlog":
                CheckBacklogProtocol(context.Root, context.Result);
                return;
            case "filesize":
                FileSizeGate.Check(context.Root, context.Result);
                return;
            case "review":
                CheckReviewTemplate(context.Root, context.Result, context.RequiredRecord);
                return;
            case "m1migrationparentcomplete":
                M1MigrationParentGate.Check(context.Root, context.Result);
                return;
        }

        RunDomain(DomainFor(mode), context);
    }

    private static void RunAll(ReviewGateContext context)
    {
        CheckBacklogProtocol(context.Root, context.Result);
        FileSizeGate.Check(context.Root, context.Result);
        CheckReviewTemplate(context.Root, context.Result, context.RequiredRecord);
        M1MigrationParentGate.Check(context.Root, context.Result);
        ArchitectureReviewGate.Check(context.Root, context.Result);
        ContentAuthoringReviewGate.Check(context.Root, context.Result);
        PresentationReviewGate.Check(context.Root, context.Result);
        RegressionReviewGate.Check(context.Root, context.Result);
    }

    private static void RunDomain(ReviewGateDomain domain, ReviewGateContext context)
    {
        switch (domain)
        {
            case ReviewGateDomain.Presentation:
                PresentationReviewGate.Check(context.Root, context.Result);
                break;
            case ReviewGateDomain.Content:
                ContentAuthoringReviewGate.Check(context.Root, context.Result);
                break;
            case ReviewGateDomain.Regression:
                RegressionReviewGate.Check(context.Root, context.Result);
                break;
            default:
                ArchitectureReviewGate.Check(context.Root, context.Result);
                break;
        }
    }

    private static ReviewGateDomain DomainFor(string mode)
    {
        if (ContainsAny(mode, "hud", "ui", "view", "camera", "fog", "grid", "vfx", "palette", "render", "display", "readability", "color", "softoldcity", "culling"))
        {
            return ReviewGateDomain.Presentation;
        }

        if (ContainsAny(mode, "unit", "build", "building", "turret", "faction", "roster", "production", "economy", "resource", "harvest", "sandbox", "skirmish", "map", "tier", "ability"))
        {
            return ReviewGateDomain.Content;
        }

        if (ContainsAny(mode, "sim", "replay", "qa", "perf", "verify", "balance", "counter", "loop", "smoke"))
        {
            return ReviewGateDomain.Regression;
        }

        return ReviewGateDomain.Architecture;
    }

    private static bool ContainsAny(string value, params string[] tokens)
    {
        return tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));
    }
}

enum ReviewGateDomain
{
    Architecture,
    Presentation,
    Content,
    Regression,
}
