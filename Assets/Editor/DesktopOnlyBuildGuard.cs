using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
namespace Qiaopi.Editor {
    // Current user scope: desktop only. Historical mobile export files remain archived.
    public sealed class DesktopOnlyBuildGuard : IPreprocessBuildWithReport {
        public int callbackOrder => -10000;
        public void OnPreprocessBuild(BuildReport report) {
            if (BuildPipeline.GetBuildTargetGroup(report.summary.platform) != BuildTargetGroup.Standalone)
                throw new BuildFailedException("侨批当前仅开发电脑版；已停用手机版构建。");
        }
        [MenuItem("侨批/导出 iPhone 测试版", true)]
        static bool DisableMobileExport() => false;
    }
}
