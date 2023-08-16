using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore.High
{
    public class EditorSceneChanger
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void CheckTools()
        {
            string sceneName = GScene.name;

            if (sceneName == "InitializationScene" || sceneName == "MainMenu" || sceneName == "GameScene")
                SceneManager.LoadScene(0);
        }
    }
}
