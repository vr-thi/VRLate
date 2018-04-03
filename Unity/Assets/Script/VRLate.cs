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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using UnityEngine;
using UnityEngine.VR;
using System;

namespace VRLate
{
	public delegate void FinishEvent ();
	public enum MonitorType
	{
		OLED_HMD,
		LCD,
		DLP

	}

	[RequireComponent (typeof(SerialCommunication))]
	public class VRLate : MonoBehaviour
	{
		[Tooltip ("Path to where all the exported CSV Files shall be added to")]
		public string outputDirectory;
		[Tooltip ("GameObject wich shall be used to output rotation values")]
		public GameObject trackedObject;
		[Tooltip ("Different monitors need to be evaluated differently")]
		public MonitorType monitorType;
		[Tooltip ("Serial port of the Microconnector")]
		public string port = "COM3";
		[Tooltip ("The baudrate of the Serial Port")]
		public int baudrate = 250000;
		[Tooltip ("Seconds before measurement starts after presed 'MEASURE' key")]
		public float countdown = 3;
		[Tooltip ("Number of frames which should virtually be delayed for the measure tool to test the accuracy")]
		public int delay = 0;

		// The highest digital value captured by the AD converter of the microcontroller
		public const int POTENTIOMETER_MAX_AD_READING = 8191;
		// Maximum angle to which the rotation platform can be rotated to and the poti shows max
		// The 3.5 is the gear translation (56 teeth and 16 teeth)
		public const float POTENTIOMETER_MAX_ANGLE = 120f;
		//1080f / 3.5f;
		// Maximum angle wich shall be used for measurement
		public const float MEASUREMENT_MAX_ANGLE = 120f;
		// The Coding for Photodiodes. Lower number means lower acuracy but better distance between codes
		public const int NUMERAL_SYSTEM = 8; 
		//The max possible value that can be displayed with the photosensors and numeral system.
		// Example: 4 Sensors and 8 values per sensor = pow(8,4)-1
		public const int PHOTOSENSOR_MAX_VALUE = (NUMERAL_SYSTEM * NUMERAL_SYSTEM * NUMERAL_SYSTEM * NUMERAL_SYSTEM) - 1;

		private enum State
		{
			Idle,
			Measure,
			Calibrate,
			CheckCalibration

		}

		private Calibrator calibrator;
		private Validator validator;
		private State state = State.Idle;
		private const int NR_OFF_MEASUREMENTS = 5000;
		private SerialCommunication serial;
		private Photosensorfield photoSensorField;
		private PhotosensorCalibration photosensorCalibration;
		private DataAnalyzer dataAnalyzer;
		private Queue<float> delayQueue;

		void Awake ()
		{
			serial = GetComponent<SerialCommunication> ();
			photoSensorField = GetComponentInChildren<Photosensorfield> ();
			calibrator = gameObject.AddComponent<Calibrator> () as Calibrator;
			validator = gameObject.AddComponent<Validator> () as Validator;
			dataAnalyzer = new DataAnalyzer ();
			delayQueue = new Queue<float> (delay + 1);
		}

		void Start ()
		{
			serial.setup (port, baudrate);
			dataAnalyzer.setMonitorType (monitorType);
			photoSensorField.setMonitorType (monitorType);
			calibrator.init (serial, photoSensorField);
			validator.init (serial, photoSensorField, dataAnalyzer);
		}

		void OnPreRender ()
		{
			if (state == State.Measure) {
				float yRot = trackedObject.transform.rotation.eulerAngles.y;
				yRot %= MEASUREMENT_MAX_ANGLE;
				float percentage = yRot / MEASUREMENT_MAX_ANGLE;
				Debug.Log ("yRot: " + yRot + " percentage: " + percentage);
				delayQueue.Enqueue( percentage );
				if (delayQueue.Count > delay) {
					float currentPercentage = delayQueue.Dequeue ();
					photoSensorField.displayRotationPercentageValue (currentPercentage);
				}
			}
		}

		void Update ()
		{
			if (Input.GetKeyDown (KeyCode.F1)) {
				Debug.Log ("Pressed F1 (Calibrate)");
				calibrate ();
			} else if (Input.GetKeyDown (KeyCode.F2)) {
				Debug.Log ("Pressed F2 (Check Calibration)");
				checkCalibration ();
			} else if (Input.GetKeyDown (KeyCode.F3)) {
				Debug.Log ("Pressed F3 (Measure)");
				measure ();
			} else if (Input.GetKeyDown (KeyCode.F4)) {
				Debug.Log ("Pressed F4 (Measure next minute)");
				measureNextMin ();
			} else if (Input.GetKeyDown (KeyCode.F5)) {
				Debug.Log ("Pressed F5 (Recenter Tracking)");
				InputTracking.Recenter ();
			}
		}

		void calibrate ()
		{
			if (state == State.Idle) {
				state = State.Calibrate;
				FinishEvent cb = finishedTaskCallback;
				StartCoroutine (calibrator.calibrate (cb));
			}
		}

		void checkCalibration ()
		{
			if (state == State.Idle) {
				photosensorCalibration = PersistenceHandler.LoadPhotosensorCalibration (); 
				if (photosensorCalibration == null) {
					Debug.LogWarning ("No Config file provided. Calibrate before taking measurements");
				} else if (state != State.Idle) {
					Debug.LogWarning ("Another task is already runnig. Wait till it' s finished before taking measurements");
				} else {
					photoSensorField.setPhotosensorCalibration (photosensorCalibration);
					dataAnalyzer.setPhotosensorCalibration (photosensorCalibration);
					state = State.CheckCalibration;
					FinishEvent cb = finishedTaskCallback;
					StartCoroutine (validator.checkCalibration (cb));
				}
			}
		}

		void finishedTaskCallback ()
		{
			Debug.Log ("Finished Task. Go back to Idle State");			
			state = State.Idle;
		}

		void measure ()
		{
			if (state == State.Idle) {
				StartCoroutine (measureCoroutine ());
			} else {
				Debug.LogWarning ("Another task is already runnig. Wait till it' s finished before taking measurements");
			}
		}

		IEnumerator measureCoroutine ()
		{
			float startTime = Time.time + countdown;
			while (Time.time < startTime) {
				float countdowon = startTime - Time.time;
				Debug.Log ("Seconds till Start: " + countdowon);
				yield return null;
			}
			photosensorCalibration = PersistenceHandler.LoadPhotosensorCalibration (); 
			if (photosensorCalibration == null) {
				Debug.LogWarning ("No Config file provided. Calibrate before taking measurements");
			} else {
				serial.setNumberOfMeasurements (NR_OFF_MEASUREMENTS);
				photoSensorField.setPhotosensorCalibration (photosensorCalibration);
				state = State.Measure;
				SerialCommunication.FinishMeasurementCallback cb = finishedMeasurementCallback;
				StartCoroutine (serial.asyncMeasure (cb));
			}
		}

		void measureNextMin ()
		{
			if (state == State.Idle) {
				photosensorCalibration = PersistenceHandler.LoadPhotosensorCalibration (); 
				if (photosensorCalibration == null) {
					Debug.LogWarning ("No Config file provided. Calibrate before taking measurements");
				} else {
					serial.setNumberOfMeasurements (NR_OFF_MEASUREMENTS);
					photoSensorField.setPhotosensorCalibration (photosensorCalibration);
					state = State.Measure;
					DateTime startTime = DateTime.UtcNow;
					startTime = startTime.AddMinutes (1);
					TimeSpan seconds = new TimeSpan (0, 0, startTime.Second);
					startTime = startTime.Subtract (seconds);
					SerialCommunication.FinishMeasurementCallback cb = finishedMeasurementCallback;
					StartCoroutine (serial.asyncMeasure (cb, startTime));
				}
			}
		}

		void finishedMeasurementCallback (ArrayList returnArray)
		{
			Debug.Log ("Finished Measurement");
			SensorData sensorData = new SensorData (returnArray);
			dataAnalyzer.setPhotosensorCalibration (photosensorCalibration);
			dataAnalyzer.generateOutputFiles (sensorData, outputDirectory);
			state = State.Idle;
		}
	}
}