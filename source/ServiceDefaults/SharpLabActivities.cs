using System.Diagnostics;

namespace Microsoft.Extensions.Hosting;

public static class SharpLabActivities {
    public static ActivitySource Source { get; } = new ActivitySource("SharpLab", "1.0");
}