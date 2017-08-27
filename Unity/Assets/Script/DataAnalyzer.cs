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
using System.IO;

namespace VRLate
{
	public class DataAnalyzer
	{
		private SensorData data;
		private PhotosensorCalibration photosensorCalibration;
		private string outputDirectory;
		private float[] trackingInputPercent;
		private float[] monitorOutputPercent;
		private int[] photosensorIntegerReadings;
		private int audioDelay;
		private MonitorType monitorType;

		private const string CSV_SEPARATOR = "\t";

		public void setMonitorType (MonitorType monitorType)
		{
			this.monitorType = monitorType;
		}

		public void setPhotosensorCalibration (PhotosensorCalibration photosensorCalibration)
		{
			this.photosensorCalibration = photosensorCalibration;
		}

		public void generateOutputFiles (SensorData data, string outputDirectory)
		{
			this.data = data;
			this.outputDirectory = outputDirectory;
			convertPhotosensorData ();
			convertPotentiometerData ();
			audioDelay = data.getAudioDelay ();
			writeCSVFiles ();
			Debug.Log ("Finished generating output files");
		}

		public int[] getPhotosensorIntegerReadings (SensorData data)
		{
			this.data = data;
			convertPhotosensorData ();
			int[] photosensorIntegerReadingsClone = (int[])photosensorIntegerReadings.Clone ();
			return photosensorIntegerReadingsClone;
		}

		private void convertPhotosensorData ()
		{
			int[][] sensorData = data.getPhotoSensorData ();
			int nrOfMeasurements = sensorData [0].Length;
			int[] tmpIntegerReadings = new int[nrOfMeasurements];
			for (int measureInterval = 0; measureInterval < nrOfMeasurements; measureInterval++) {
				int intOutput = calculateMonitorOutput (measureInterval, sensorData);
				tmpIntegerReadings [measureInterval] = intOutput;
			}

			//TODO helper function
			if (monitorType == MonitorType.OLED_HMD) {
				photosensorIntegerReadings = new int[tmpIntegerReadings.Length];
				List<int> tmpArr = new List<int> ();
				for (int i = 0; i < photosensorIntegerReadings.Length; i++) {
					int currentReading = tmpIntegerReadings [i];
					if (currentReading >= 0) { 
						tmpArr.Add (currentReading);
					} else if (tmpArr.Count > 0) {
						int max = Mathf.Max (tmpArr.ToArray ());
						int maxArrIdx = tmpArr.IndexOf (max);
						for (int tmpArrIdx = tmpArr.Count; tmpArrIdx >= 0; tmpArrIdx--) {
							if (tmpArrIdx == maxArrIdx) {
								photosensorIntegerReadings [i - (tmpArr.Count - tmpArrIdx)] = max;
							} else {
								photosensorIntegerReadings [i - (tmpArr.Count - tmpArrIdx)] = -1;
							}
						}
						tmpArr.Clear ();
					} else {
						photosensorIntegerReadings [i] = -1;
					}
				}

				// Clear first and last readings since it can also be an incomplete frame
				for (int i = 0; i < 5; i++) {
					photosensorIntegerReadings [i] = -1;
					photosensorIntegerReadings [photosensorIntegerReadings.Length - i - 1] = -1;
				}



			} else if (monitorType == MonitorType.LCD) {
				photosensorIntegerReadings = (int[])tmpIntegerReadings.Clone ();
			} else {
				photosensorIntegerReadings = (int[])tmpIntegerReadings.Clone ();//TODO other monitor types
			}

			// Convert integer reading into percentage value
			monitorOutputPercent = new float[photosensorIntegerReadings.Length];
			for (int measurePos = 0; measurePos < photosensorIntegerReadings.Length; measurePos++) {
				int currIntegerReading = photosensorIntegerReadings [measurePos];
				if (currIntegerReading > 0) {
					monitorOutputPercent [measurePos] = (float)currIntegerReading / VRLate.PHOTOSENSOR_MAX_VALUE;
				} else {
					monitorOutputPercent [measurePos] = -1f;
				}
			}

		}

		private int calculateMonitorOutput (int measureInterval, int[][] sensorData)
		{
			string encodedSensorReading = "";
			for (int sensorID = 0; sensorID < 4; sensorID++) {
				int sensorReading = sensorData [sensorID] [measureInterval];
				int sensorBlackLevel = photosensorCalibration.blackLevel.sensor [sensorID].photosensorAnalogReading;
				// Some OLED HMDs turn of the screen between frames. Return -1 to indicate those black level readings
				if (sensorReading < sensorBlackLevel) {
					return -1;
				} 
				int encodedSensorValue = -1;
				int tmpMinDistance = 0;
				// check what brightness code fits best for the current sensor reading
				for (int brightnessCode = 0; brightnessCode < VRLate.NUMERAL_SYSTEM; brightnessCode++) {
					int sensorBrightnessCode = photosensorCalibration.brightnessCodes [brightnessCode].sensor [sensorID].photosensorAnalogReading;
					int distance = Mathf.Abs (sensorBrightnessCode - sensorReading);
					// First iteration
					if (encodedSensorValue == -1) {
						tmpMinDistance = distance;
						encodedSensorValue = 0;
					} else if (distance < tmpMinDistance) {
						tmpMinDistance = distance;
						encodedSensorValue = brightnessCode;
					}
				}	
				encodedSensorReading = encodedSensorReading + encodedSensorValue.ToString ();
			}
			int intOutput = (int)NumeralSystem.ArbitraryToDecimal (encodedSensorReading, VRLate.NUMERAL_SYSTEM);
			return intOutput;
		}

		private void convertPotentiometerData ()
		{
			int[] potentiometerData = data.getPotentiometerData ();
			trackingInputPercent = new float[potentiometerData.Length];
			for (int i = 0; i < potentiometerData.Length; i++) {
				int potValue = potentiometerData [i];
				float potDegrees = (float)((float)potValue * VRLate.POTENTIOMETER_MAX_ANGLE) / VRLate.POTENTIOMETER_MAX_AD_READING;
				float relValue = potDegrees / VRLate.MEASUREMENT_MAX_ANGLE;
				trackingInputPercent [i] = relValue;
			}
		}

		private void writeCSVFiles ()
		{
			if (!Directory.Exists (outputDirectory)) {
				Debug.LogWarning ("Output direcotry '" + outputDirectory + "' does not exist. Can't write output files");
				return;
			}
			int measureIntervals = trackingInputPercent.Length;
			string[] linesTrackingInput = new string[measureIntervals + 1];
			string[] linesMonitorOutput = new string[measureIntervals + 1];
			string[] lineAudioDelay = new string[1];

			// CSV Header
			linesTrackingInput [0] = "Interval" + CSV_SEPARATOR + "Value";
			linesMonitorOutput [0] = "Interval" + CSV_SEPARATOR + "Value";
			// Data
			for (int interval = 1; interval <= measureIntervals; interval++) {
				linesTrackingInput [interval] = interval + CSV_SEPARATOR + trackingInputPercent [interval - 1];
				linesMonitorOutput [interval] = interval + CSV_SEPARATOR + monitorOutputPercent [interval - 1];
			}
			lineAudioDelay [0] = audioDelay.ToString ();

			File.WriteAllLines (outputDirectory + "/TrackingInput.csv", linesTrackingInput);
			File.WriteAllLines (outputDirectory + "/MonitorOutput.csv", linesMonitorOutput);
			File.WriteAllLines (outputDirectory + "/AudioDelay.txt", lineAudioDelay);
		}
	}
}