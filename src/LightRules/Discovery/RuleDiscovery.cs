namespace LightRules.Discovery;

/// <summary>
/// Discovery helper for finding rule metadata. Uses the global registry which is
/// populated automatically via ModuleInitializers - no reflection required.
/// </summary>
public static class RuleDiscovery
{
    /// <summary>
    /// Discover all registered rule metadata, ordered by priority then name.
    /// Rules are automatically registered when their assembly loads via ModuleInitializer.
    /// </summary>
    public static IEnumerable<RuleMetadata> Discover()
    {
        return GlobalRuleRegistry.GetAll();
    }
}
