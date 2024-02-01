using UnityEngine;
using UnityEditor;
using UnityEditor.Compilation;

[InitializeOnLoad]
public static class CompileTimer
{
    private static double _startTime;

    static CompileTimer()
    {
        CompilationPipeline.compilationStarted += OnCompilationStarted;
        CompilationPipeline.compilationFinished += OnCompilationFinished;
    }

    private static void OnCompilationStarted(object obj)
    {
        _startTime = EditorApplication.timeSinceStartup;
    }

    private static void OnCompilationFinished(object obj)
    {
        double endTime = EditorApplication.timeSinceStartup;
        double duration = endTime - _startTime;
        Debug.Log($"本次编译耗时：{duration:F2}s");
    }
}