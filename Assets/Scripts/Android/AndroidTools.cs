using UnityEngine;
using UnityEngine.Android;

namespace GameCore.High
{
    /// <summary>
    /// 调用 Android 一些方法的整理
    /// </summary>
    public static class AndroidTools
    {
        /// <summary>
        /// 通过包名调用其他软件
        /// </summary>
        /// <param name="pkgName">应用包名</param>
        /// <param name="activity">AndroidJavaObject</param>
        public static void OpenPackage(string pkgName, AndroidJavaObject activity = null)
        {
            if (activity == null)
            {
                AndroidJavaClass UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                activity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            }

            using (AndroidJavaObject joPackageManager = activity.Call<AndroidJavaObject>("getPackageManager"))
            {
                using (AndroidJavaObject joIntent = joPackageManager.Call<AndroidJavaObject>("getLaunchIntentForPackage", pkgName))
                {
                    if (null != joIntent)
                    {
                        activity.Call("startActivity", joIntent);
                    }
                    else
                    {
                        Debug.Log("未安装此软件：" + pkgName);
                    }
                }
            }
        }

        public static bool OpenFileManager(AndroidJavaObject activity = null)
        {
            try
            {
                AndroidTools.OpenPackage("bin.mt.plus");
                return true;
            }
            catch
            {
                try
                {
                    AndroidTools.OpenPackage("com.estrongs.android.pop");
                    return true;
                }
                catch
                {
                    try
                    {
                        AndroidTools.OpenPackage("com.android.filemanager");
                        return true;
                    }
                    catch
                    {
                        try
                        {
                            AndroidTools.OpenPackage("com.android.fileexplorer");
                            return true;
                        }
                        catch
                        {
                            try
                            {
                                AndroidTools.OpenPackage("com.android.documentsui");
                                return true;
                            }
                            catch
                            {
                                try
                                {
                                    AndroidTools.OpenPackage("com.google.android.apps.nbu.files");
                                    return true;
                                }
                                catch
                                {

                                }
                            }
                        }
                    }

                }
            }

            return false;
        }
    }
}
//相关说明
//a.AndroidJavaClass对应着Android里面的Java类，而AndroidJavaObject对应着Android里面实例化的对象。
//b.一定要切记C#里的String和Java的String不是一码事，所以调用Android方法时如果需要传字符串为参数时，不能直接给个字符串，应该给个Java里的String，例如 new AndroidJavaObject("java.lang.String","你想传的字符串");
//c.由于AndroidJavaClass对应的是类，所以一般用之来调用对应的类的静态变量（GetStatic<Type>）或者静态方法(CallStatic<Type>("functionName", param1, param2,....));其中的Type为返回类型，注意是Java的返回类型不是C#的，一般整型和布尔型是通用的，其他的如果不清除可以统一写返回类型为AndroidJavaObject，当然没有返回类型的不需要写Type。
//d.AndroidJavaObject对应的是实例对象，所以用new方法给其初始化时要注意说明其是哪个类的实例对象。再比如刚才那个例子： AndroidJavaObject javaString = new AndroidJavaObject("java.lang.String", "字符串的值");

