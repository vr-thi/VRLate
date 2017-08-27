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
	public class Validator : MonoBehaviour
	{
		private Photosensorfield photosensorfield;
		private SerialCommunication serial;
		private DataAnalyzer dataAnalyzer;
		private SensorData currSensorReadings;
		private const int VALIDATE_NR_OFF_MEASUREMENTS = 50;

		public void init (SerialCommunication serial, Photosensorfield photosensorfield, DataAnalyzer dataAnalyzer)
		{
			this.serial = serial;
			this.photosensorfield = photosensorfield;
			this.dataAnalyzer = dataAnalyzer;
		}

		// Start photosensor validation
		public IEnumerator checkCalibration (FinishEvent callback)
		{
			serial.setNumberOfMeasurements (VALIDATE_NR_OFF_MEASUREMENTS);
			int allOne = (int)NumeralSystem.ArbitraryToDecimal ("1111", VRLate.NUMERAL_SYSTEM);
			for (int currInteger = 0; currInteger <= VRLate.PHOTOSENSOR_MAX_VALUE; currInteger += allOne) {
				string encodedString = NumeralSystem.DecimalToArbitrary (currInteger, VRLate.NUMERAL_SYSTEM);
				// Padd with leading zeros
				encodedString = encodedString.PadLeft (4, '0');
				photosensorfield.displayEncodedString (encodedString);
				yield return StartCoroutine (takeMeasurement ());
				int[] intReadings = dataAnalyzer.getPhotosensorIntegerReadings (currSensorReadings);
				bool readExpectedValue = false;
				foreach (int currReading in intReadings) {
					if (currReading == currInteger) {
						readExpectedValue = true;
					}
					if (!(currReading == -1 || currReading == currInteger)) {
						Debug.LogWarning ("Calibration was not correct. Expected -1 or " + currInteger +
						" (Encoded: " + NumeralSystem.DecimalToArbitrary (currInteger, VRLate.NUMERAL_SYSTEM) +
						")" + " but reading was " + currReading +
						" (Encoded: " + NumeralSystem.DecimalToArbitrary (currReading, VRLate.NUMERAL_SYSTEM) + ")");
						callback ();
						yield break;
					}
				}
				if (!readExpectedValue) {
					Debug.LogWarning ("Did not read the expected vaule '" + currInteger + "' in the sensor Radings. Only -1");
					callback ();
					yield break;
				}
				Debug.Log ("Successfully checked integer: " + currInteger);
			}
			Debug.Log ("Finished validation. Everything is Okay");
			callback ();
		}

		private IEnumerator takeMeasurement ()
		{
			// Wait for next Frame
			yield return null;
			yield return serial.asyncMeasure (finishedMeasurementCallback); 
		}

		private void finishedMeasurementCallback (ArrayList returnArray)
		{
			currSensorReadings = new SensorData (returnArray);
		}
	}
}