// using UnityEditor;
// using UnityEditor.Android;

// public class AddPermissions : IPostGenerateGradleAndroidProject
// {
//     public int callbackOrder { get { return 0; } }

//     public void OnPostGenerateGradleAndroidProject(string path)
//     {
//         string manifestPath = path + "/src/main/AndroidManifest.xml";
//         string manifestContent = System.IO.File.ReadAllText(manifestPath);

//         if (!manifestContent.Contains("android.permission.READ_EXTERNAL_STORAGE"))
//         {
//             manifestContent = manifestContent.Replace("</manifest>", "    <uses-permission android:name=\"android.permission.READ_EXTERNAL_STORAGE\" />\n</manifest>");
//         }

//         if (!manifestContent.Contains("android.permission.WRITE_EXTERNAL_STORAGE"))
//         {
//             manifestContent = manifestContent.Replace("</manifest>", "    <uses-permission android:name=\"android.permission.WRITE_EXTERNAL_STORAGE\" />\n</manifest>");
//         }

//         AddRequestLegacyExternalStorageAttribute(manifestPath);
//         System.IO.File.WriteAllText(manifestPath, manifestContent);
//     }

//     private void AddRequestLegacyExternalStorageAttribute(string manifestPath)
//     {
//         // 加载AndroidManifest.xml文件
//         var manifest = new System.Xml.XmlDocument();
//         manifest.Load(manifestPath);

//         // 获取根节点
//         var rootNode = manifest.DocumentElement;

//         // 添加requestLegacyExternalStorage属性
//         rootNode.SetAttribute("android:requestLegacyExternalStorage", "true");

//         // 保存修改后的AndroidManifest.xml文件
//         manifest.Save(manifestPath);
//     }
// }