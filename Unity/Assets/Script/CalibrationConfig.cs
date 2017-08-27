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
using UnityEngine;

namespace VRLate
{
	[System.Serializable]
	public class PhotosensorCalibration
	{
		[System.Serializable]
		public struct CalibrationPair
		{
			public int monitorBrightness;
			public int photosensorAnalogReading;
		}

		[System.Serializable]
		public class PhotoSensorCalibration
		{
			// The Photosensor consists of 4 single sensors
			public CalibrationPair[] sensor;

			//ctor
			public PhotoSensorCalibration ()
			{			
				sensor = new CalibrationPair[4];
			}

			// Helper function for debugging
			public void printValues ()
			{
				for (int i = 0; i < sensor.Length; i++) {
					Debug.Log ("Sensor_" + (i + 1) + " >> Monitor: " + sensor [i].monitorBrightness
					+ " Analog Value: " + sensor [i].photosensorAnalogReading);
				}
			}
		}

		public PhotoSensorCalibration blackLevel;
		public PhotoSensorCalibration[] brightnessCodes;

		private System.DateTime created;

		public PhotosensorCalibration ()
		{
			created = System.DateTime.Now;
			blackLevel = new PhotoSensorCalibration ();
			brightnessCodes = new PhotoSensorCalibration[VRLate.NUMERAL_SYSTEM];
			for (int i = 0; i < brightnessCodes.Length; ++i) {
				brightnessCodes [i] = new PhotoSensorCalibration ();
			}
		}

		public System.DateTime getCreationDateTime ()
		{
			return created;
		}
	}
}