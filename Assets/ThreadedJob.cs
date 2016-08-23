using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class ThreadedJob {
	public virtual void Start() { }
}

public class ThreadedJob<T> : ThreadedJob {
	static Queue<ThreadedJob> jobQueue;
	static List<ThreadedJob> inProgress;
	static int usableProcessors = Environment.ProcessorCount - 1;

	public static IEnumerator Do(Func<T> f, Action<T> onComplete) {
		var job = new ThreadedJob<T>();
		job.Setup(f);
		AddJob(job);
		while (!job.isDone)
			yield return null;
		JobDone(job);
		onComplete(job.output);
	}

	private static void AddJob(ThreadedJob job) {
		if (jobQueue == null)
			jobQueue = new Queue<ThreadedJob>();
		jobQueue.Enqueue(job);
		DoNext();
	}

	private static void JobDone(ThreadedJob job)
	{
		inProgress.Remove(job);
		DoNext();
	}

	private static void DoNext()
	{
		if (inProgress == null)
			inProgress = new List<ThreadedJob>();
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
