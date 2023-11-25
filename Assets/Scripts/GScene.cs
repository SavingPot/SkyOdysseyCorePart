using GameCore.High;
using SP.Tools;
using SP.Tools.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore
{
    public static class GScene
    {
        public static Action<int> BeforeLoad = s => { };
        public static Action<Scene> AfterChanged = scene =>
        {
            active = scene;
            name = active.name;
            index = active.buildIndex;

            Performance.CollectMemory();

            Debug.Log($"场景变为: {name} ({index})");

            //遍历所有已加载所有 Type
            lock (ModFactory.mods)
                foreach (var mod in ModFactory.mods)
                    foreach (var type in mod.typeData)
                        //如果 Type 拥有特性 CreateAfterSceneLoadAttribute
                        if (AttributeGetter.TryGetAttribute(type.type, typeof(CreateAfterSceneLoadAttribute), out var attribute))
                            //如果排除的场景没有一个为当前场景
                            if (!((CreateAfterSceneLoadAttribute)attribute).exceptScenes.Any(p => p == name))
                                Tools.NewObjectToComponent(type.type);

            if (Client.isClient)
            {
                Client.WhenIsConnected(() =>
                {
                    if (Client.connection != null)
                        Client.Send<NMClientChangeScene>(new(name));
                });
            }
        };

        public static void LastAsync() => LoadAsync(lastIndex);

        /// <summary>
        /// 加载下一个场景.
        /// </summary>
        /// <remarks>
        /// Load the next scene.
        /// </remarks>
        public static void Next() => Load(nextIndex);

        /// <summary>
        /// 动态加载下一个场景.
        /// </summary>
        /// <remarks>
        /// Load the next scene async.
        /// </remarks>
        public static void NextAsync() => LoadAsync(nextIndex);

        public static void Load(int index)
        {
            BeforeLoad(index);

            SceneManager.LoadScene(index);
        }

        public static void LoadAsync(int index)
        {
            BeforeLoad(index);

            CoroutineStarter.Do(IELoadAsync(index));
        }

        /// <summary>
        /// 重新加载场景.
        /// </summary>
        /// <remarks>
        /// Reload this scene.
        /// </remarks>
        public static void ReloadScene() => Load(index);

        /// <summary>
        /// 异步重载场景.
        /// </summary>
        /// <remarks>
        /// Reload this scene async.
        /// </remarks>
        public static void ReloadSceneAsync() => LoadAsync(index);

        /// <summary>
        /// 动态加载场景.
        /// </summary>
        /// <remarks>
        /// Load scene async.
        /// </remarks>
        /// <param name="index"></param>
        /// <returns></returns>
        public static IEnumerator IELoadAsync(int index)   //tsi = target scene index ; tsn = target scene name
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(index);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                yield return null;
            }

            operation.allowSceneActivation = true;
            yield break;
        }

        public static Scene active { get; internal set; }

        /// <summary>
        /// 获取上一个场景的 index.
        /// </summary>
        /// <remarks>
        /// Get the index of next scene.
        /// </remarks>
        public static int lastIndex => active.buildIndex - 1;

        /// <summary>
        /// 获取下一个场景的 index.
        /// </summary>
        /// <remarks>
        /// Get the index of next scene.
        /// </remarks>
        public static int nextIndex => active.buildIndex + 1;

        /// <summary>
        /// 获取当前场景的名字.
        /// </summary>
        /// <remarks>
        /// Get the name of this scene.
        /// </remarks>
        public static string name { get; internal set; }

        /// <summary>
        /// 获取当前场景的名字.
        /// </summary>
        /// <remarks>
        /// Get the name of this scene.
        /// </remarks>
        public static string nextSceneName => SceneManager.GetSceneByBuildIndex(nextIndex).name;

        /// <summary>
        /// 获取当前场景在 BuildSettings 中的 Index.
        /// </summary>
        /// <remarks>
        /// Get the index of this scene in BuildSettings.
        /// </remarks>
        public static int index { get; internal set; }
    }
}
