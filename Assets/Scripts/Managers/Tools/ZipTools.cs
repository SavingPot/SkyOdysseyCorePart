using ICSharpCode.SharpZipLib.Zip;
using SP.Tools;
using System;
using System.IO;
using UnityEngine;

namespace GameCore.High
{
    public static class ZipTools
    {
        #region ZipCallback
        public class ZipCallback
        {
            public ZipCallback(Action<bool> onFinished)
            {
                this.onFinished = onFinished;
            }
            /// <summary>
            /// 压缩单个文件或文件夹前执行的回调
            /// </summary>
            /// <param name="entry"></param>
            /// <returns>如果返回true，则压缩文件或文件夹，反之则不压缩文件或文件夹</returns>
            public virtual bool OnPreZip(ZipEntry entry)
            {
                return true;
            }


            /// <summary>
            /// 压缩单个文件或文件夹后执行的回调
            /// </summary>
            /// <param name="entry"></param>
            public Action<ZipEntry> onPostZip;


            /// <summary>
            /// 压缩执行完毕后的回调
            /// </summary>
            /// <param name="result">true表示压缩成功，false表示压缩失败</param>
            public Action<bool> onFinished;
        }
        #endregion


        #region UnzipCallback
        public class UnzipCallback
        {
            public UnzipCallback(Action<bool> onFinished)
            {
                this.onFinished = onFinished;
            }
            /// <summary>
            /// 解压单个文件或文件夹前执行的回调
            /// </summary>
            /// <param name="entry"></param>
            /// <returns>如果返回true，则压缩文件或文件夹，反之则不压缩文件或文件夹</returns>
            public virtual bool OnPreUnzip(ZipEntry entry)
            {
                return true;
            }


            /// <summary>
            /// 解压单个文件或文件夹后执行的回调
            /// </summary>
            /// <param name="entry"></param>
            public Action<ZipEntry> onPostUnzip;


            /// <summary>
            /// 解压执行完毕后的回调
            /// </summary>
            /// <param name="result">true表示解压成功，false表示解压失败</param>
            public Action<bool> onFinished;
        }
        #endregion



        /// <summary>
        /// 压缩文件和文件夹
        /// </summary>
        /// <param name="fileOrDirectoryArray">文件夹路径和文件名</param>
        /// <param name="outputPath">压缩后的输出路径文件名</param>
        /// <param name="password">压缩密码</param>
        /// <param name="zipCallback">ZipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool Zip(string fileOrDirectoryPath, string outputPath, ZipCallback zipCallback = null) => Zip(new[] { fileOrDirectoryPath }, outputPath, zipCallback);

        /// <summary>
        /// 压缩文件和文件夹
        /// </summary>
        /// <param name="fileOrDirectoryArray">文件夹路径和文件名</param>
        /// <param name="outputPath">压缩后的输出路径文件名</param>
        /// <param name="password">压缩密码</param>
        /// <param name="zipCallback">ZipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool Zip(string[] fileOrDirectoryArray, string outputPath, ZipCallback zipCallback = null)
        {
            Debug.Log("尝试压缩");

            foreach (var item in fileOrDirectoryArray)
                Debug.Log(item);

            if (fileOrDirectoryArray == null || string.IsNullOrEmpty(outputPath))
            {
                zipCallback?.onFinished(false);
                return false;
            }

            ZipOutputStream zipOutputStream = new(File.Create(outputPath));
            zipOutputStream.SetLevel(6);    // 压缩质量和压缩速度的平衡点

            for (int index = 0; index < fileOrDirectoryArray.Length; ++index)
            {
                bool result = false;
                string fileOrDirectory = fileOrDirectoryArray[index];
                if (Directory.Exists(fileOrDirectory))
                    result = ZipDirectory(fileOrDirectory, string.Empty, zipOutputStream, zipCallback);
                else if (File.Exists(fileOrDirectory))
                    result = ZipFile(fileOrDirectory, string.Empty, zipOutputStream, zipCallback);

                if (!result)
                {
                    zipCallback?.onFinished(false);
                    return false;
                }
            }

            zipOutputStream.Finish();
            zipOutputStream.Close();

            zipCallback?.onFinished(true);

            Debug.Log("成功压缩");

            foreach (var item in fileOrDirectoryArray)
                Debug.Log(item);

            return true;
        }


        /// <summary>
        /// 压缩文件
        /// </summary>
        /// <param name="filePathName">文件路径名</param>
        /// <param name="parentRelPath">要压缩的文件的父相对文件夹</param>
        /// <param name="zipOutputStream">压缩输出流</param>
        /// <param name="zipCallback">ZipCallback对象，负责回调</param>
        /// <returns></returns>
        private static bool ZipFile(string filePathName, string parentRelPath, ZipOutputStream zipOutputStream, ZipCallback zipCallback = null)
        {
            ZipEntry entry = null;
            using FileStream fileStream = File.OpenRead(filePathName);

            try
            {
                string entryName = parentRelPath + '/' + IOTools.GetParentPath(filePathName);

                entry = new(entryName)
                {
                    DateTime = DateTime.Now
                };

                if (zipCallback != null && !zipCallback.OnPreZip(entry))
                    return true;

                byte[] buffer = new byte[fileStream.Length];
                fileStream.Read(buffer, 0, buffer.Length);
                fileStream.Close();

                entry.Size = buffer.Length;

                zipOutputStream.PutNextEntry(entry);
                zipOutputStream.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Tools.LogException(ex, "压缩文件时出错: ");
                return false;
            }
            finally
            {
                fileStream?.Close();
            }

            zipCallback?.onPostZip?.Invoke(entry);
            return true;
        }


        /// <summary>
        /// 压缩文件夹
        /// </summary>
        /// <param name="path">要压缩的文件夹</param>
        /// <param name="parentRelPath">要压缩的文件夹的父相对文件夹</param>
        /// <param name="zipOutputStream">压缩输出流</param>
        /// <param name="zipCallback">ZipCallback对象，负责回调</param>
        /// <returns></returns>
        private static bool ZipDirectory(string path, string parentRelPath, ZipOutputStream zipOutputStream, ZipCallback zipCallback = null)
        {
            ZipEntry entry;
            try
            {
                string entryName = Path.Combine(parentRelPath, Path.GetFileName(path) + '/');
                entry = new(entryName)
                {
                    DateTime = DateTime.Now,
                    Size = 0
                };


                if (zipCallback != null && !zipCallback.OnPreZip(entry))
                    return true;


                zipOutputStream.PutNextEntry(entry);
                zipOutputStream.Flush();


                string[] files = Directory.GetFiles(path);
                for (int index = 0; index < files.Length; ++index)
                    ZipFile(files[index], Path.Combine(parentRelPath, Path.GetFileName(path)), zipOutputStream, zipCallback);
            }
            catch (Exception ex)
            {
                Tools.LogException(ex, "压缩文件夹时出错");
                return false;
            }


            string[] directories = Directory.GetDirectories(path);
            for (int index = 0; index < directories.Length; ++index)
            {
                if (!ZipDirectory(directories[index], Path.Combine(parentRelPath, Path.GetFileName(path)), zipOutputStream, zipCallback))
                    return false;
            }

            zipCallback?.onPostZip?.Invoke(entry);
            return true;
        }


        /// <summary>
        /// 解压Zip包
        /// </summary>
        /// <param name="filePath">Zip包的文件路径名</param>
        /// <param name="outputPath">解压输出路径</param>
        /// <param name="password">解压密码</param>
        /// <param name="unzipCallback">UnzipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool UnzipFile(string filePath, string outputPath, UnzipCallback unzipCallback = null)
        {
            if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(outputPath))
            {
                unzipCallback?.onFinished?.Invoke(false);
                return false;
            }

            try
            {
                return UnzipFile(File.OpenRead(filePath), outputPath, unzipCallback);
            }
            catch (Exception ex)
            {
                Tools.LogException(ex, "解压文件时出错: ");

                unzipCallback?.onFinished?.Invoke(false);
                return false;
            }
        }


        /// <summary>
        /// 解压Zip包
        /// </summary>
        /// <param name="fileBytes">Zip包字节数组</param>
        /// <param name="outputPath">解压输出路径</param>
        /// <param name="password">解压密码</param>
        /// <param name="unzipCallback">UnzipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool UnzipFile(byte[] fileBytes, string outputPath, UnzipCallback unzipCallback = null)
        {
            if ((null == fileBytes) || string.IsNullOrEmpty(outputPath))
            {
                unzipCallback?.onFinished?.Invoke(false);
                return false;
            }

            bool result = UnzipFile(new MemoryStream(fileBytes), outputPath, unzipCallback);

            if (!result)
                unzipCallback?.onFinished?.Invoke(false);

            return result;
        }


        /// <summary>
        /// 解压Zip包
        /// </summary>
        /// <param name="inputStream">Zip包输入流</param>
        /// <param name="outputPath">解压输出路径</param>
        /// <param name="password">解压密码</param>
        /// <param name="unzipCallback">UnzipCallback对象，负责回调</param>
        /// <returns></returns>
        public static bool UnzipFile(Stream source, string outputPath, UnzipCallback unzipCallback = null)
        {
            if (source == null || outputPath.IsNullOrEmpty())
            {
                unzipCallback?.onFinished?.Invoke(false);
                return false;
            }

            outputPath = Path.GetFullPath(outputPath);

            //创建文件目录
            IOTools.CreateDirectoryIfNone(outputPath);

            using ZipInputStream decompressor = new(source);
            ZipEntry entry;

            while ((entry = decompressor.GetNextEntry()) != null)
            {
                if (string.IsNullOrEmpty(entry.Name))
                    continue;

                if (unzipCallback != null && !unzipCallback.OnPreUnzip(entry))
                    continue;   // 过滤

                string name = entry.Name;
                if (entry.IsDirectory && entry.Name.StartsWith("\\"))
                    name = entry.Name.Replace("\\", string.Empty);

                string filePath = Path.Combine(outputPath, name);
                string directoryPath = Path.GetDirectoryName(filePath);

                if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                if (entry.IsDirectory)
                {
                    continue;
                }

                byte[] data = new byte[2048];
                using FileStream streamWriter = File.Create(filePath);
                int bytesRead;
                while ((bytesRead = decompressor.Read(data, 0, data.Length)) > 0)
                {
                    streamWriter.Write(data, 0, bytesRead);
                }
                unzipCallback?.onPostUnzip?.Invoke(entry);
            }

            unzipCallback?.onFinished?.Invoke(true);
            return true;
        }
    }
}
