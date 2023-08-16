using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Linq;

public class Loom : MonoBehaviour
{
    public static int maxThreads = 8;
    static int numThreads;

    private static Loom _current;
    //private int _count;
    public static Loom Current
    {
        get
        {
            Initialize();
            return _current;
        }
    }

    void Awake()
    {
        _current = this;
        initialized = true;
    }

    static bool initialized;

    public static void Initialize()
    {
        if (!initialized)
        {
            if (!Application.isPlaying)
                return;

            initialized = true;
            var g = new GameObject("Loom");
            _current = g.AddComponent<Loom>();

#if !ARTIST_BUILD
            DontDestroyOnLoad(g);
#endif
        }

    }
    public struct NoDelayedQueueItem
    {
        public Action<object> action;
        public object param;
    }

    private List<NoDelayedQueueItem> actions = new();
    public struct DelayedQueueItem
    {
        public float time;
        public Action<object> action;
        public object param;
    }
    private List<DelayedQueueItem> delayed = new();

    List<DelayedQueueItem> currentDelayed = new();

    public static void QueueOnMainThread(Action<object> taction, object param)
    {
        QueueOnMainThread(taction, param, 0f);
    }
    public static void QueueOnMainThread(Action<object> taction, object param, float time)
    {
        if (time != 0)
        {
            lock (Current.delayed)
            {
                Current.delayed.Add(new DelayedQueueItem { time = Time.time + time, action = taction, param = param });
            }
        }
        else
        {
            lock (Current.actions)
            {
                Current.actions.Add(new NoDelayedQueueItem { action = taction, param = param });
            }
        }
    }

    public static Thread RunAsync(Action a)
    {
        Initialize();
        while (numThreads >= maxThreads)
        {
            Thread.Sleep(100);
        }
        Interlocked.Increment(ref numThreads);
        ThreadPool.QueueUserWorkItem(RunAction, a);
        return null;
    }

    private static void RunAction(object action)
    {
        try
        {
            ((Action)action)();
        }
        catch
        {
        }
        finally
        {
            Interlocked.Decrement(ref numThreads);
        }
    }


    void OnDisable()
    {
        if (_current == this)
        {
            _current = null;
        }
    }


    List<NoDelayedQueueItem> currentActions = new();

    public void Update()
    {
        if (actions.Count > 0)
        {
            lock (actions)
            {
                currentActions.Clear();
                currentActions.AddRange(actions);
                actions.Clear();
            }
            for (int i = 0; i < currentActions.Count; i++)
            {
                currentActions[i].action(currentActions[i].param);
            }
        }

        if (delayed.Count > 0)
        {
            lock (delayed)
            {
                currentDelayed.Clear();
                currentDelayed.AddRange(delayed.Where(d => d.time <= Time.time));
                for (int i = 0; i < currentDelayed.Count; i++)
                {
                    delayed.Remove(currentDelayed[i]);
                }
            }

            for (int i = 0; i < currentDelayed.Count; i++)
            {
                currentDelayed[i].action(currentDelayed[i].param);
            }
        }
    }
}