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
using System.Collections.Generic;
using UnityEngine;

namespace VRLate
{
	public class SensorData
	{
		private ArrayList rawData;
		// First Index is the sensor ID. Second the received value counter
		private int[][] photoSensorData;
		private int[] potentiometerData;
		private int audioDelay;

		public SensorData (ArrayList data)
		{
			rawData = data;
			parseRawData ();
		}

		public void printRawData ()
		{
			foreach (var item in rawData) {
				Debug.Log (rawData);
			}
		}

		public int[][] getPhotoSensorData ()
		{
			return photoSensorData;
		}

		public int[] getPotentiometerData ()
		{
			return potentiometerData;
		}

		public int getAudioDelay ()
		{
			return audioDelay;
		}

		private void parseRawData ()
		{
			if (rawData == null) {
				Debug.LogError ("No Data received. Please check connection to Microcontroller");
				return;
			}
			audioDelay = (int)rawData [0];
			int nrOfSensorReadings = (rawData.Count - 1) / 5;
			Debug.Log ("Number of Sensorreadings: " + nrOfSensorReadings);
			photoSensorData = new int[4] [];
			potentiometerData = new int[nrOfSensorReadings];
			for (int i = 0; i < 4; ++i) {
				photoSensorData [i] = new int[nrOfSensorReadings];
			}

			for (int i = 0; i < nrOfSensorReadings; ++i) {
				potentiometerData [i] = (int)rawData [5 * i + 1];
				photoSensorData [0] [i] = (int)rawData [5 * i + 2];
				photoSensorData [1] [i] = (int)rawData [5 * i + 3];
				photoSensorData [2] [i] = (int)rawData [5 * i + 4];
				photoSensorData [3] [i] = (int)rawData [5 * i + 5];
			}
		}

		// Hide default ctor
		private SensorData ()
		{
		}
	}
}