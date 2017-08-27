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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRLate
{
	public class Calibrator : MonoBehaviour
	{
		private const int BLACKLEVEL_SAFETY = 200;

		private Photosensorfield photoSensorField;
		private SerialCommunication serial;
		private PhotosensorCalibration photosensorCalibration;
		private int[][] currPhotosensorReadings;
		private const int CALIBRATE_NR_OFF_MEASUREMENTS = 100;

		public void init (SerialCommunication serial, Photosensorfield photoSensorField)
		{
			this.serial = serial;
			this.photoSensorField = photoSensorField;
		}

		// Start sensorCalibration
		public IEnumerator calibrate (FinishEvent callback)
		{
			photosensorCalibration = new PhotosensorCalibration ();
			serial.setNumberOfMeasurements (CALIBRATE_NR_OFF_MEASUREMENTS);

			// Blacklevel
			yield return StartCoroutine (takeMeasurement (0));
			for (int senID = 0; senID < 4; senID++) {
				photosensorCalibration.blackLevel.sensor [senID].photosensorAnalogReading = Mathf.Max (currPhotosensorReadings [senID]) + BLACKLEVEL_SAFETY;
				photosensorCalibration.blackLevel.sensor [senID].monitorBrightness = -1;
			}
			Debug.Log ("Set BlackLevels");
			photosensorCalibration.blackLevel.printValues ();

			// WhiteLevel
			yield return StartCoroutine (takeMeasurement (255));
			for (int senID = 0; senID < 4; senID++) {
				int median = medianIgnoringBlackLevel (currPhotosensorReadings [senID], photosensorCalibration.blackLevel.sensor [senID].photosensorAnalogReading);
				if (median < 0) {
					callback ();
					yield break;
				} else {
					photosensorCalibration.brightnessCodes [VRLate.NUMERAL_SYSTEM - 1].sensor [senID].photosensorAnalogReading = median;
					photosensorCalibration.brightnessCodes [VRLate.NUMERAL_SYSTEM - 1].sensor [senID].monitorBrightness = 255;
				}
			}
			Debug.Log ("Set WhiteLevels");
			photosensorCalibration.brightnessCodes [VRLate.NUMERAL_SYSTEM - 1].printValues ();

			// Single Steps
			int ctrLen = VRLate.NUMERAL_SYSTEM - 2;
			int[] counter = { ctrLen, ctrLen, ctrLen, ctrLen };
			int[] stepSize = new int[4];
			for (int senID = 0; senID < 4; senID++) {
				int maxBrightness = photosensorCalibration.brightnessCodes [VRLate.NUMERAL_SYSTEM - 1].sensor [senID].photosensorAnalogReading;
				int minBlacklevel = photosensorCalibration.blackLevel.sensor [senID].photosensorAnalogReading;
				stepSize [senID] = Mathf.RoundToInt ((maxBrightness - minBlacklevel) / VRLate.NUMERAL_SYSTEM);
			}
			for (byte currBrighness = 254; currBrighness > 0; --currBrighness) {
				if (Mathf.Max (counter) < 0) {
					PersistenceHandler.SavePhotosensorCalibration (photosensorCalibration);
					Debug.Log ("Successfully Finished Calibration");
					callback ();
					yield break;
				}
				yield return StartCoroutine (takeMeasurement (currBrighness));

				for (int senID = 0; senID < 4; senID++) {
					int currAnalogMedian = medianIgnoringBlackLevel (currPhotosensorReadings [senID], photosensorCalibration.blackLevel.sensor [senID].photosensorAnalogReading);
					if (currAnalogMedian < 0) {
						callback ();
						yield break;
					}
					int nextBrightnessThreshold = (photosensorCalibration.brightnessCodes [counter [senID] + 1].sensor [senID].photosensorAnalogReading - stepSize [senID]);
					if (counter [senID] >= 0 && currAnalogMedian <= nextBrightnessThreshold) {
						Debug.Log ("Found brightness code '" + counter [senID] + "' for Sensor '" + (senID + 1) + "' Monitorbrightness: " + currBrighness + " AnalogReading: " + currAnalogMedian);
						photosensorCalibration.brightnessCodes [counter [senID]].sensor [senID].monitorBrightness = currBrighness;
						photosensorCalibration.brightnessCodes [counter [senID]].sensor [senID].photosensorAnalogReading = currAnalogMedian;
						--counter [senID];
					}
				}
			}
			Debug.LogError ("Could not calibrate Sensor successfully. Check if sensor is attached correct and try again");
			callback ();
		}

		private IEnumerator takeMeasurement (byte brightness)
		{
			photoSensorField.setFullAreaBrightness (brightness);
			// Wait for next Frame
			yield return null;
			yield return serial.asyncMeasure (finishedMeasurementCallback); 
		}

		private void finishedMeasurementCallback (ArrayList returnArray)
		{
			SensorData sensorData = new SensorData (returnArray);
			currPhotosensorReadings = sensorData.getPhotoSensorData ();
		}

		// Returns the median of an array but ignors black frames.
		// This is necessary because HMDs like the OCR turn of the screen after displaying a frame
		private int medianIgnoringBlackLevel (int[] array, int blackLevel)
		{
			List<int> noNoiseValues = new List<int> ();
			foreach (int ele in array) {
				if (ele > blackLevel) {
					noNoiseValues.Add (ele);
				}
			}
			// Median
			noNoiseValues.Sort ();
			if (!(noNoiseValues.Count >= 1)) {
				Debug.LogError ("No Median found for current array. Reached BlackLevel. Check if photosensor is attached correctly");
				return -1;
			}
			return noNoiseValues [noNoiseValues.Count / 2];
		}
	}
}