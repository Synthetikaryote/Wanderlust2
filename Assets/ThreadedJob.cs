using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class ThreadedJob {
	public virtual void Start() { }
	protected int id = 0;
}

public class ThreadedJob<T> : ThreadedJob {
	protected static Queue<ThreadedJob<T>> jobQueue;
	protected static List<ThreadedJob<T>> inProgress;
	protected static int usableProcessors = Environment.ProcessorCount - 1;
	protected static int jobID = 0;

	public static IEnumerator Do(Func<T> f, Action<T> onComplete) {
		var job = new ThreadedJob<T>();
		job.Setup(f);
		AddJob(job);
		job.id = jobID;
		++jobID;
		while (!job.isDone)
		{
			yield return null;
		}
		Debug.Log("Finished job with thread id" + job.thread.ManagedThreadId);
		JobDone(job);
		onComplete(job.output);
	}

	private static void AddJob(ThreadedJob<T> job) {
		if (jobQueue == null)
			jobQueue = new Queue<ThreadedJob<T>>();
		jobQueue.Enqueue(job);
		DoNext();
	}

	private static void JobDone(ThreadedJob<T> job)
	{
		inProgress.Remove(job);
		DoNext();
	}

	private static void DoNext()
	{
		if (inProgress == null)
			inProgress = new List<ThreadedJob<T>>();
		if (inProgress.Count < usableProcessors && jobQueue.Count > 0)
		{
			var job = jobQueue.Dequeue();
			inProgress.Add(job);
			job.Start();
		}
	}

	public bool isDone = false;
	Thread thread = null;
	Func<T> f = null;
	T output = default(T);

	public virtual void Setup(Func<T> f) {
		this.f = f;
	}

	public override void Start() {
		thread = new Thread(Run);
		Debug.Log("Starting job with thread id " + thread.ManagedThreadId);
		thread.Start();
	}

	public virtual void Abort() {
		thread.Abort();
	}

	void Run() {
		output = f();
		isDone = true;
	}
}
