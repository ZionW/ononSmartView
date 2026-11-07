namespace OnonSmartView
{

internal static class Compatibility
{
    public static double Clamp(double value, double min, double max) => System.Math.Max(min, System.Math.Min(max, value));
    public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
}

}
#if RHINO7
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
    [System.AttributeUsage(System.AttributeTargets.All)]
    internal sealed class RequiredMemberAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.All)]
    internal sealed class CompilerFeatureRequiredAttribute : System.Attribute
    {
        public CompilerFeatureRequiredAttribute(string name) { FeatureName = name; }
        public string FeatureName { get; }
        public bool IsOptional { get; set; }
    }
}
#endif
