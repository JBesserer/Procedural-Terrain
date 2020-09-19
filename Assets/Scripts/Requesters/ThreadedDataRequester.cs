using System;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour
{
    static ThreadedDataRequester instance;
    private Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

    private void Awake() 
    {
        instance = FindObjectOfType<ThreadedDataRequester>();
    }
    public static void RequestData(Func<object> generateData, Action<object> callback)
    {
        ThreadStart threadStart = delegate {
            instance.DataThread(generateData, callback);
        };

        new Thread(threadStart).Start();
    }

    private void DataThread(Func<object> generateData, Action<object> callback)
    {
        object data = generateData();
        //Locks the thread when the method reaches that point so that no other thread can execute it as well, will have to wait it's turn
        lock (dataQueue)
        {
            dataQueue.Enqueue(new ThreadInfo(callback,data));
        }
    }

    private void Update() {
        if(dataQueue.Count > 0)
        {
            for (int i = 0; i < dataQueue.Count; i++)
            {
                ThreadInfo threadInfo = dataQueue.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }
        }
    }

    private struct ThreadInfo
    {
        public readonly Action<object> callback;
        public readonly object parameter;

        public ThreadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
