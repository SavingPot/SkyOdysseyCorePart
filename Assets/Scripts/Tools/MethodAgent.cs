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
        public static void TryQueueOnMainThread(Action action, Action<Exception> onFailed = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            Loom.QueueOnMainThread(_ => TryRun(action, onFailed), null);
        }

        /// <summary>
        /// 在下一帧尝试执行 Action
        /// </summary>
        /// <param name="action"></param>
        /// <exception cref="ArgumentNullException"></exception>
        [ChineseName("尝试队列主线程")]
        public static void DebugQueueOnMainThread(Action action, Action<Exception> onFailed = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            Loom.QueueOnMainThread(_ => DebugRun(action, onFailed), null);
        }

        public static Exception TryRun(Action action, Action<Exception> onFailed = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            try
            {
                action();
            }
            catch (Exception ex)
            {
                onFailed?.Invoke(ex);

                return ex;
            }

            return null;
        }

        public static Exception DebugRun(Action action, Action<Exception> onFailed = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action), $"{MethodGetter.GetLastAndCurrentMethodPath()} {nameof(action)} 值不能为空");

            try
            {
                action();
            }
            catch (Exception ex)
            {
                Debug.LogError($"{MethodGetter.GetLastAndCurrentMethodPath()}: 发生了错误, 具体内容:\n\n{Tools.HighlightedStackTrace(ex)}\n\n");
                onFailed?.Invoke(ex);

                return ex;
            }

            return null;
        }



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

        public static Thread RunThread(ThreadStart start)
        {
            var thread = new Thread(start);
            thread.Start();
            return thread;
        }

        public static Thread RunBGThread(ThreadStart start)
        {
            var thread = new Thread(start) { IsBackground = true };
            thread.Start();
            return thread;
        }

        public static Thread RunThreadEndless(ThreadStart start) => RunThread(() =>
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
