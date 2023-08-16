using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using SP.Tools;
using System.IO;
using System.Linq;

public class PathAutoDelete
{
    static string[] dlls = new[]
    {
        "Sirenix.OdinInspector.Attributes",
        "Sirenix.Serialization.Config",
        "Sirenix.Serialization",
        "Sirenix.Utilities",
        "System.Drawing",
        "Unity.Services.Core.Analytics",
        "Unity.Services.Core.Configuration",
        "Unity.Services.Core.Device",
        "Unity.Services.Core",
        "Unity.Services.Core.Environments",
        "Unity.Services.Core.Environments.Internal",
        "Unity.Services.Core.Internal",
        "Unity.Services.Core.Networking",
        "Unity.Services.Core.Registration",
        "Unity.Services.Core.Scheduler",
        "Unity.Services.Core.Telemetry",
        "Unity.Services.Core.Threading"
    };

    [PostProcessBuildAttribute(1000)]
    public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
    {
        Debug.Log("开始处理导出文件");

        string parent = IOTools.GetParentPath(pathToBuiltProject);

        switch (target)
        {
            //Windows 平台
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                /* -------------------------------- 删除崩溃处理程序 -------------------------------- */
                File.Delete(Path.Combine(parent, "UnityCrashHandler64.exe"));

                /* ------------------------------- 删除 Burst 日志 ------------------------------ */
                string burst = Path.Combine(parent, $"{Application.productName}_BurstDebugInformation_DoNotShip");
                if (Directory.Exists(burst))
                    IOTools.DeleteDir(burst);

                /* --------------------------- 删除 Mono 中无用的 etc文件夹 -------------------------- */
                IOTools.DeleteDir(Path.Combine(parent, "MonoBleedingEdge", "etc"));

                /* -------------------------------- 删除无用 dll -------------------------------- */
                string dllPath = Path.Combine(parent, $"{Application.productName}_Data", "Managed");
                foreach (var item in IOTools.GetFilesInFolderIncludingChildren(dllPath, true))
                {
                    string fileName = IOTools.GetFileName(item, false);

                    if (dlls.Any(p => p == fileName))
                    {
                        File.Delete(item);
                    }
                }

                /* ------------------------------ 把版本复制到 Latest ----------------------------- */
                string latestPath = Path.Combine(IOTools.GetParentPath(parent), "Latest");
                if (Directory.Exists(latestPath))
                    IOTools.DeleteDir(latestPath);
                IOTools.CopyDir(parent, latestPath);

                break;
        }
    }
}