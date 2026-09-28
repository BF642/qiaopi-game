using UnityEditor;
namespace Qiaopi.Editor { public static class HistoricalGroundChecks {
[MenuItem("Qiaopi/Validate regional maps")] public static void Run(){ RegionChecks.Run(); }
} }
