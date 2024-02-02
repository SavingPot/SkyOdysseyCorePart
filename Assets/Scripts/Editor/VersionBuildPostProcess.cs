using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using SP.Tools;
using System.IO;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class VersionBuildPreprocess : IPreprocessBuildWithReport
{
    internal static string oriEditorPath = Path.Combine(Application.dataPath, "StreamingAssets", "sole_assets", "mods", "ori");
    public int callbackOrder { get { return 0; } }


    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("开始预处理");

        // 获取构建信息
        BuildSummary summary = report.summary;
        //summary.outputPath：构建的输出路径。
        //summary.platform：构建的目标平台。
        //summary.options：构建的选项。
        //summary.result：构建的结果（成功、失败等）。



        /* -------------------------------------------------------------------------- */
        /*                                在这里执行打包前处理的操作                               */
        /* -------------------------------------------------------------------------- */



        //删除编辑器里的 ori
        if (Directory.Exists(oriEditorPath))
        {
            IOTools.DeleteDir(oriEditorPath);
            File.Delete($"{oriEditorPath}.meta");
        }

        switch (summary.platform)
        {
            case BuildTarget.Android:
                {
                    //把 ori 复制到编辑器里
                    var oriSourcePath = "D:/MakeGames/GameProject/ori_copy_for_editor/sole_assets/mods/ori";

                    IOTools.CopyDir(oriSourcePath, oriEditorPath);
                }
                break;
        }
    }
}

public class VersionBuildPostProcess
{
    static readonly string[] dllsToDelete = new[]
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

    [PostProcessBuild(1000)]
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
                //File.Delete(Path.Combine(parent, "UnityCrashHandler64.exe"));

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

                    if (dllsToDelete.Any(p => p == fileName))
                    {
                        File.Delete(item);
                    }
                }

                /* --------------------------------- 复制 ori --------------------------------- */
                var oriSourcePath = "D:/MakeGames/GameProject/ori_copy_for_editor/sole_assets/mods/ori";
                var oriTargetPath = Path.Combine(parent, $"{Application.productName}_Data", "StreamingAssets", "sole_assets", "mods", "ori");

                if (Directory.Exists(oriTargetPath))
                    IOTools.DeleteDir(oriTargetPath);

                IOTools.CopyDir(oriSourcePath, oriTargetPath);

                /* ------------------------------ 把版本复制到 Latest ----------------------------- */
                string latestPath = Path.Combine(IOTools.GetParentPath(parent), "Latest");
                if (Directory.Exists(latestPath))
                    IOTools.DeleteDir(latestPath);
                IOTools.CopyDir(parent, latestPath);

                break;
        }
    }
}
