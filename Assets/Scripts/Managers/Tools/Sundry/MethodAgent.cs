using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using SP.Tools;
using Cysharp.Threading.Tasks;

namespace GameCore
{
    [ChineseName("方法代理")]
    public static class MethodAgent
    {
        public static Action updates = () => { };
        public static Action fixedUpdates = () => { };

        public static SynchronizationContext mainThreadSyncContext = SynchronizationContext.Current;
        public static Thread mainThread = Thread.CurrentThread;
        public static int mainThreadId = Thread.CurrentThread.ManagedThreadId;

        public static string[] launchArgs = Environment.GetCommandLineArgs();


        public static bool IsOnMainThread()
        {
            return Thread.CurrentThread.ManagedThreadId == mainThreadId;
        }

        /// <summary>
        /// 立马在主线程执行 Action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ChineseName("在主线程执行")]
        public static void RunOnMainThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            mainThreadSyncContext.Send(new(_ => action()), null);
        }

        /// <summary>
        /// 立马在主线程执行 Action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ChineseName("在主线程执行")]
        public static void RunOnMainThread(Action<object> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            mainThreadSyncContext.Send(new(action), null);
        }

        /// <summary>
        /// 立马尝试在主线程执行 Action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ChineseName("在主线程执行")]
        public static void TryRunOnMainThread(Action action, object param = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            mainThreadSyncContext.Send(new(_ => TryRun(action)), null);
        }

        /// <summary>
        /// 在下一帧执行 Action
        /// </summary>
        /// <param name="action"></param>
        /// <param name="param"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ChineseName("队列主线程")]
        public static void QueueOnMainThread(Action<object> action, object param = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            Loom.QueueOnMainThread(action, param);
        }

        /// <summary>
        /// 在下一帧执行 Action, 使用 Action 性能可能比 Action<object> 略差?
        /// </summary>
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ChineseName("队列主线程")]
        public static void QueueOnMainThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            Loom.QueueOnMainThread(_ => action(), null);
        }

        /// <summary>
        /// 在下一帧尝试执行 Action
        /// </summary>
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ChineseName("尝试队列主线程")]
        public static void TryQueueOnMainThread(Action action, bool logError = false, Action<Exception> onFailed = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            Loom.QueueOnMainThread(_ => TryRun(action, logError, onFailed), null);
        }

        [ChineseName("尝试运行")]
        public static Exception TryRun(Action action, bool logError = false, Action<Exception> onFailed = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            try
            {
                action();
            }
            catch (NullReferenceException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 引用值为空, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (ArgumentNullException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: {ex.ParamName} 的值为空, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (ArgumentOutOfRangeException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 索引 {ex.ActualValue}[{ex.ParamName}] 超出范围, 必须为非负数且小于集合的大小, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (ArgumentException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: {ex.ParamName} 的值不正确, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (IndexOutOfRangeException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 索引超出范围, 必须为非负数且小于集合的大小, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (DirectoryNotFoundException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: IO 操作失败, 没有找到指定的文件夹, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (FileNotFoundException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: IO 操作失败, 没有找到指定的文件 {ex.FileName}, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (FileLoadException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: IO 操作失败, 无法加载文件 {ex.FileName}, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (PathTooLongException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: IO 操作失败, 目录过长, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (EndOfStreamException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: IO 操作失败, 可能是因为已经到达 Stream 的末尾而还在继续读数据, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (IOException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: IO 操作失败, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (InvalidCastException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 类型转换失败, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (MissingMethodException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 发生错误, 未找到方法, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (FieldAccessException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 字段访问失败, 可能是没有足够的访问权限，也可能是要访问的字段不存在, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (MethodAccessException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 方法访问失败, 可能是没有足够的访问权限，也可能是要访问的方法不存在, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (MissingMemberException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 成员访问失败, 它并不存在, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (MemberAccessException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 成员访问失败, 可能是没有足够的访问权限，也可能是要访问的成员不存在, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (DivideByZeroException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 试图在十进制运算中除以零, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (NotFiniteNumberException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 浮点数运算中出现无穷大或者非负值的数 {ex.OffendingNumber}, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (ArrayTypeMismatchException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 数组储存的元素类型不正确, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (InvalidOperationException ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 无效的操作, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }
            catch (Exception ex)
            {
                if (logError)
                {
                    Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 发生了错误, 具体内容:\n\n{ex}\n\n");
                    onFailed?.Invoke(ex);
                }

                return ex;
            }

            return null;
        }

        [ChineseName("添加更新")] public static void AddUpdate(IUpdate update) => updates += update.Update;
        [ChineseName("添加更新")] public static void AddUpdate(Action update) => updates += update;
        [ChineseName("删除更新")] public static void RemoveUpdate(IUpdate update) => updates -= update.Update;
        [ChineseName("删除更新")] public static void RemoveUpdate(Action update) => updates -= update;

        [ChineseName("添加固定更新")] public static void AddFixedUpdate(IFixedUpdate fixedUpdate) => fixedUpdates += fixedUpdate.FixedUpdate;
        [ChineseName("添加固定更新")] public static void AddFixedUpdate(Action fixedUpdate) => fixedUpdates += fixedUpdate;
        [ChineseName("删除固定更新")] public static void RemoveFixedUpdate(IFixedUpdate fixedUpdate) => fixedUpdates -= fixedUpdate.FixedUpdate;
        [ChineseName("删除固定更新")] public static void RemoveFixedUpdate(Action fixedUpdate) => fixedUpdates -= fixedUpdate;



        public static async void CallFramesLater(int frameCount, Action action)
        {
            await UniTask.DelayFrame(frameCount);

            action();
        }

        public static async void CallNextFrame(Action action)
        {
            await UniTask.NextFrame();

            action();
        }

        public static void RunThread(ThreadStart start) => new Thread(start).Start();

        public static void RunBGThread(ThreadStart start) => new Thread(start) { IsBackground = true }.Start();

        public static void RunThreadEndless(ThreadStart start) => RunThread(() =>
        {
            while (true)
                start();
        });

        public static void RunBGThreadEndless(ThreadStart start)
        {
            Thread thread = new(() =>
            {
                while (true)
                    start();
            })
            {
                IsBackground = true
            };
            thread.Start();
        }

        public static bool RunThreadPool(WaitCallback start) => ThreadPool.QueueUserWorkItem(start);

        public static void RunTask(Action start) => new Task(start).Start();

        public static Task RunTaskRef(Action start)
        {
            Task task = new(start);
            task.Start();
            return task;
        }

        public static void RunTaskEndless(Action start) => RunTask(() =>
        {
            while (true)
                start();
        });

        public static async void CallUntil(Func<bool> func, Action action)
        {
            while (!func())
                await UniTask.NextFrame();

            action();
        }
    }
}
