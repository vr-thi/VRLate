﻿/*******************************************************************************
Copyright 2017 Technische Hochschule Ingolstadt

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, 
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions: 

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
IN THE SOFTWARE.
***********************************************************************************/

using System.Collections;

public class ThreadJob
{
	public bool isDone;

	private bool done = false;
	private object handle = new object ();
	private System.Threading.Thread thread = null;

	public bool IsDone {
		get {
			bool tmp;
			lock (handle) {
				tmp = done;
			}
			return tmp;
		}
		set {
			lock (handle) {
				done = value;
			}
		}
	}

	public virtual void Start ()
	{
		thread = new System.Threading.Thread (Run);
		thread.Start ();
	}

	public virtual void Abort ()
	{
		thread.Abort ();
	}

	protected virtual void ThreadFunction ()
	{
	}

	protected virtual void OnFinished ()
	{
	}

	public virtual bool Update ()
	{
		if (IsDone) {
			OnFinished ();
			return true;
		}
		return false;
	}

	public IEnumerator WaitFor ()
	{
		while (!Update ()) {
			yield return null;
		}
	}

	private void Run ()
	{
		ThreadFunction ();
		IsDone = true;
	}
}