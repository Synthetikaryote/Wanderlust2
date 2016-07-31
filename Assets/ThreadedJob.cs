using UnityEngine;
using System;
using System.Collections;
using System.Threading;

public class ThreadedJob<T> {
	public static IEnumerator Do(Func<T> f, Action<T> onComplete) {
		var job = new ThreadedJob<T>();
		job.Start(f);
		while (!job.isDone)
			yield return null;
		onComplete(job.output);
	}

	public bool isDone = false;
	Thread thread = null;
	Func<T> f = null;
	T output = default(T);

	public virtual void Start(Func<T> f) {
		thread = new Thread(Run);
		this.f = f;
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
