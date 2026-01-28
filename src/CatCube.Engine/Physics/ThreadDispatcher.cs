using BepuUtilities;
using BepuUtilities.Memory;
using System;
using System.Threading;

namespace CatCube.Engine.Physics;

public class SimpleThreadDispatcher : IThreadDispatcher, IDisposable
{
    int threadCount;
    Task[] workers;
    AutoResetEvent[] workerSignals;
    AutoResetEvent completionSignal;
    volatile Action<int>? work;
    volatile int workersRemaining;

    public SimpleThreadDispatcher(int threadCount)
    {
        this.threadCount = threadCount;
        workers = new Task[threadCount - 1];
        workerSignals = new AutoResetEvent[threadCount - 1];
        completionSignal = new AutoResetEvent(false);
        bufferPools = new BufferPool[threadCount];
        
        for (int i = 0; i < bufferPools.Length; ++i)
        {
            bufferPools[i] = new BufferPool();
        }
        
        for (int i = 0; i < workers.Length; ++i)
        {
            workerSignals[i] = new AutoResetEvent(false);
            int localIndex = i;
            workers[i] = new Task(() => WorkerLoop(localIndex));
            workers[i].Start();
        }
    }

    void WorkerLoop(int workerIndex)
    {
        while (true)
        {
            workerSignals[workerIndex].WaitOne();
            if (disposed)
                return;
            work?.Invoke(workerIndex);
            if (Interlocked.Decrement(ref workersRemaining) == 0)
            {
                completionSignal.Set();
            }
        }
    }

    public void DispatchWorkers(Action<int> workerBody, int maximumThreadCount = -1)
    {
        if (maximumThreadCount > 0)
        {
            // Simple implementation doesn't support limiting thread count dynamically nicely,
            // but for this engine we just run full speed.
        }
        
        work = workerBody;
        workersRemaining = threadCount - 1;
        for (int i = 0; i < workerSignals.Length; ++i)
        {
            workerSignals[i].Set();
        }
        // Run one on this thread too
        workerBody(threadCount - 1);
        
        if (workersRemaining > 0) // If any other threads are running
            completionSignal.WaitOne();
    }

    volatile bool disposed;
    
    // Bepu 2.4 requires thread memory pools
    BufferPool[] bufferPools;

    public BufferPool GetThreadMemoryPool(int workerIndex)
    {
        return bufferPools[workerIndex];
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            for (int i = 0; i < workerSignals.Length; ++i)
            {
                workerSignals[i].Set();
            }
            
            // Dispose pools
            if (bufferPools != null)
            {
                foreach (var pool in bufferPools)
                    pool?.Clear();
            }
        }
    }

    public int ThreadCount => threadCount;
}
