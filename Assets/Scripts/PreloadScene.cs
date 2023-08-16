using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using GameCore.High;
using System.IO;
using SP.Tools;

namespace GameCore
{
    public class PreloadScene : MonoBehaviour
    {
#if UNITY_EDITOR

#endif

        private void Awake()
        {
#if UNITY_EDITOR

#endif

#if UNITY_ANDROID
            // void Request(string str)
            // {
            //     if (!AndroidRuntimePermissions.CheckPermission(str))
            //     {
            //         var result = AndroidRuntimePermissions.RequestPermission(str);

            //         switch (result)
            //         {
            //             case AndroidRuntimePermissions.Permission.Granted:
            //                 break;

            //             case AndroidRuntimePermissions.Permission.ShouldAsk:
            //                 Request(str);
            //                 break;
            //         }
            //     }
            // }

            // Request("android.permission.READ_MEDIA_IMAGES");
            // Request("android.permission.READ_MEDIA_AUDIO");
            // Request("android.permission.MANAGE_EXTERNAL_STORAGE");
#endif


            SceneManager.LoadScene(1);
        }
    }
}
