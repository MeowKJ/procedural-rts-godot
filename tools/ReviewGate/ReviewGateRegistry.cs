sealed record ReviewGateContext(string Root, GateResult Result);

static class ReviewGateRegistry
{
    public static bool IsKnownMode(string mode, string root)
    {
        return ReviewGateModeCatalog.IsKnown(mode, root);
    }

    public static string DescribeKnownModes(string root)
    {
        return ReviewGateModeCatalog.Describe(root);
    }

    public static void Run(string mode, ReviewGateContext context)
    {
        ReviewGateDomainRunner.Run(mode, context);
    }
}
