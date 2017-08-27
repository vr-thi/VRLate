/*******************************************************************************
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
using System.IO.Ports;
using System;
using UnityEngine;

namespace VRLate
{
	public class SerialAsyncReader : ThreadJob
	{
		public SerialPort InStream;
		public ArrayList OutReturnData;

		private bool timeout;
		private static float TIMEOUT_S = 30f;

		protected override void ThreadFunction ()
		{
			OutReturnData = new ArrayList ();
			int measuredValues = -1;
			bool firstArgument = true; // First arg is the audio delay
			bool secondArgument = false; // Second arg indicates how many values where measured
			int receivedValues = 0;
			DateTime initialTime = DateTime.Now;
			DateTime nowTime;
			TimeSpan diff = default(TimeSpan);
			string dataString = null;

			while (true) {
				nowTime = DateTime.Now;
				diff = nowTime - initialTime;

				if (measuredValues != -1 && receivedValues >= measuredValues) {
					timeout = false;
					break;
				} else if (diff.Seconds >= TIMEOUT_S) {
					timeout = true;
					break;
				}	

				try {
					dataString = InStream.ReadLine ();
				} catch (TimeoutException) {
					dataString = null;
				}

				if (dataString != null) {
					int dataInt = int.Parse (dataString);
					if (firstArgument) {
						OutReturnData.Add (int.Parse (dataString));
						firstArgument = false;
						secondArgument = true;
					} else if (secondArgument) {
						measuredValues = dataInt;
						secondArgument = false;
					} else {
						OutReturnData.Add (int.Parse (dataString));
						++receivedValues;
					}
					continue;
				} else {
					continue;
				}	 
			} 
		}

		protected override void OnFinished ()
		{
			Debug.Log ("Assync Read thread finished.");
			if (timeout) {
				Debug.LogError ("Timeout AssyncRead. Check connection to Microcontroller");
			} 
		}
	}
}