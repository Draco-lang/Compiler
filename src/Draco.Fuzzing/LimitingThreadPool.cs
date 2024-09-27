using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Draco.Fuzzing;

/// <summary>
/// A thread-pool that blocks, until a thread is available to start working.
/// </summary>
internal sealed class LimitingThreadPool
{
    private sealed class PooledThread
    {
        public Thread Thread { get; }
        public AutoResetEvent WorkAvailable { get; }
        public Action? DoWork { get; set; }

        private readonly LimitingThreadPool pool;

        public PooledThread(LimitingThreadPool pool)
        {
            this.Thread = new Thread(this.Run);
            this.WorkAvailable = new AutoResetEvent(false);
            this.pool = pool;
        }

        private void Run()
        {
            while (true)
            {
                try
                {
                    this.WorkAvailable.WaitOne();
                    this.DoWork?.Invoke();
                }
                finally
                {
                    this.pool.threads.Enqueue(this);
                    this.pool.semaphore.Release();
                }
            }
        }

        public void StartIfNeeded()
        {
            if (this.Thread.ThreadState != ThreadState.Unstarted) return;
            this.Thread.Start();
        }
    }

    private readonly ConcurrentQueue<PooledThread> threads = new();
    private readonly SemaphoreSlim semaphore;

    public LimitingThreadPool(int threadCount)
    {
        this.semaphore = new SemaphoreSlim(threadCount, threadCount);
        for (var i = 0; i < threadCount; i++)
        {
            var thread = new PooledThread(this);
            this.threads.Enqueue(thread);
        }
    }

    public void QueueWork(Action action, CancellationToken cancellationToken)
    {
        this.semaphore.Wait(cancellationToken);
        if (this.threads.TryDequeue(out var thread))
        {
            thread.StartIfNeeded();
            thread.DoWork = action;
            thread.WorkAvailable.Set();
        }
        else
        {
            this.semaphore.Release();
        }
    }
}
