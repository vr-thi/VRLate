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
using UnityEngine.UI;
using UnityEngine.Assertions;

namespace VRLate
{
	public class Photosensorfield : MonoBehaviour
	{
		public Image photosensorBackground;
		public Image photosensorArea_1;
		public Image photosensorArea_2;
		public Image photosensorArea_3;
		public Image photosensorArea_4;

		private PhotosensorCalibration photosensorCalibration;
		private bool displayGreenOnly = false;

		void Start ()
		{
			// let all images render in foreground (Effect Layer)
			photosensorBackground.material.renderQueue = 4000;
			photosensorArea_1.material.renderQueue = 4000;
			photosensorArea_2.material.renderQueue = 4000;
			photosensorArea_3.material.renderQueue = 4000;
			photosensorArea_4.material.renderQueue = 4000;
		}

		public void displayRotationPercentageValue (float rotationPercentage)
		{
			Assert.IsTrue (rotationPercentage >= 0.0f && rotationPercentage <= 1.0f);
			int roationInt = Mathf.RoundToInt ((rotationPercentage * VRLate.PHOTOSENSOR_MAX_VALUE));
			Assert.IsTrue (roationInt >= 0 && roationInt <= VRLate.PHOTOSENSOR_MAX_VALUE);
			// Convert to other numeral system
			string numeralString = NumeralSystem.DecimalToArbitrary (roationInt, VRLate.NUMERAL_SYSTEM);
			// Padd with leading zeros
			numeralString = numeralString.PadLeft (4, '0');
			displayEncodedString (numeralString);
		}

		public void displayEncodedString (string encodedString)
		{
			Assert.IsTrue (encodedString.Length == 4);
			Debug.Log ("Display encoded string: " + encodedString);
			for (int digit = 0; digit < 4; ++digit) {
				byte byteValue = (byte)char.GetNumericValue (encodedString [digit]);
				setBrightness (digit + 1, convertEncodedValueToIntensity (digit + 1, byteValue));
			}
		}

		public void setPhotosensorCalibration (PhotosensorCalibration config)
		{
			this.photosensorCalibration = config;
		}

		public void setMonitorType (MonitorType type)
		{
			displayGreenOnly = (type == MonitorType.DLP);
			if (type == MonitorType.OLED_HMD) {
				this.gameObject.transform.Rotate (new Vector3 (180f, 180f, 0));
			}
		}

		// Set Brightness between 0-255 of whole area
		public void setFullAreaBrightness (byte intensity)
		{
			for (int i = 0; i < 4; ++i) {
				setBrightness (i + 1, intensity);
			}
		}

		// Based on Calibration file. Input from 0 to 7;
		private byte convertEncodedValueToIntensity (int PhotosensorAreaID, byte encodedInput)
		{
			Assert.IsTrue (encodedInput >= 0 && encodedInput < VRLate.NUMERAL_SYSTEM);
			PhotosensorCalibration.CalibrationPair[] calibrationForEncodedInput = photosensorCalibration.brightnessCodes [encodedInput].sensor;
			byte output = (byte)calibrationForEncodedInput [PhotosensorAreaID - 1].monitorBrightness;
			return output;
		}

		// intensity from 0 - 255
		private void setBrightness (int PhotosensorAreaID, byte intensity)
		{
			byte intensityRedAndBlue = intensity;
			if (displayGreenOnly) {
				intensityRedAndBlue = 0;
			}

			switch (PhotosensorAreaID) {
			case 1: 
				photosensorArea_1.color = new Color32 (intensityRedAndBlue, intensity, intensityRedAndBlue, 255);
				break;
			case 2: 
				photosensorArea_2.color = new Color32 (intensityRedAndBlue, intensity, intensityRedAndBlue, 255);
				break;
			case 3: 
				photosensorArea_3.color = new Color32 (intensityRedAndBlue, intensity, intensityRedAndBlue, 255);
				break;
			case 4: 
				photosensorArea_4.color = new Color32 (intensityRedAndBlue, intensity, intensityRedAndBlue, 255);
				break;
			default:
				Debug.LogError ("Unknow PhotosensorAreaID '" + PhotosensorAreaID + "' Must be between 1 and 4");
				break;
			}
		}
	}
}