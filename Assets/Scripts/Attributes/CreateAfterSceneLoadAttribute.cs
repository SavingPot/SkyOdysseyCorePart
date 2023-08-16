using System;

namespace GameCore
{
    /// <summary>
    /// 切换场景时如果场景名不等于 exceptScene 中的任意一个则创建新的物体并添加脚本
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class CreateAfterSceneLoadAttribute : Attribute
    {
        public string[] exceptScenes;

        public CreateAfterSceneLoadAttribute()
        {

        }

        public CreateAfterSceneLoadAttribute(string exceptScene)
        {
            this.exceptScenes = new[] { exceptScene };
        }

        public CreateAfterSceneLoadAttribute(string[] exceptScenes)
        {
            this.exceptScenes = exceptScenes;
        }
    }
}
