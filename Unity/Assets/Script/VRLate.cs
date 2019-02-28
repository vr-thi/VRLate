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
using UnityEngine.VR;

namespace VRLate
{
    public delegate void FinishEvent();

    public enum MonitorType
    {
        OLED_HMD,
        LCD,
        DLP
    }

    [RequireComponent(typeof(SerialCommunication))]
    public class VRLate : MonoBehaviour
    {
        [Tooltip("Path to where all the exported CSV Files shall be added to")]
        [SerializeField] private string _outputDirectory;

        [Tooltip("GameObject which shall be used to output rotation values")]
        [SerializeField] private GameObject _trackedObject;

        [Tooltip("Different monitors need to be evaluated differently")]
        [SerializeField] private MonitorType _monitorType;

        [Tooltip("Serial port of the microcontroller")]
        [SerializeField] private string _port = "COM3";

        [Tooltip("The baud rate of the Serial Port")]
        [SerializeField] private int _baudrate = 250000;

        [Tooltip("Seconds before measurement starts after pressed 'MEASURE' key")]
        [SerializeField] private float _countdown = 3;

        [Tooltip("Number of frames which should virtually be delayed for the measure tool to test the accuracy")]
        [SerializeField] private int _delay = 0;

        [Tooltip("Use this value for OLED displays. Throw away all readings below this value.")]
        [SerializeField] private int _blackLevelVoltag = 200;

        // The highest digital value captured by the AD converter of the microcontroller
        public const int MICROCONTROLLER_MAX_AD_READING = 8191;

        // Maximum angle to which the rotation platform can be rotated to and the poti shows max
        // The 3.5 is the gear translation (56 teeth and 16 teeth)
        public const float POTENTIOMETER_MAX_ANGLE = 120f;

        //1080f / 3.5f;
        // Maximum angle which shall be used for measurement
        public const float MEASUREMENT_MAX_ANGLE = 120f;

        private enum State
        {
            Idle,
            Measure
        }

        private State state = State.Idle;
        private const int NR_OFF_MEASUREMENTS = 5000;
        private SerialCommunication serial;
        private Photosensorfield photoSensorField;
        private DataAnalyzer dataAnalyzer;
        private Queue<float> delayQueue;

        void Awake()
        {
            serial = GetComponent<SerialCommunication>();
            photoSensorField = GetComponentInChildren<Photosensorfield>();
            dataAnalyzer = new DataAnalyzer();
            delayQueue = new Queue<float>(_delay + 1);
        }

        void Start()
        {
            serial.Setup(_port, _baudrate);
            dataAnalyzer.SetMonitorType(_monitorType);
            photoSensorField.SetMonitorType(_monitorType);
            dataAnalyzer.BlackLevelVoltage = _blackLevelVoltag;
        }

        void OnPreRender()
        {
            if (state == State.Measure)
            {
                float yRot = _trackedObject.transform.rotation.eulerAngles.y;
                yRot %= MEASUREMENT_MAX_ANGLE;
                float percentage = yRot / MEASUREMENT_MAX_ANGLE;
                Debug.Log("yRot: " + yRot + " percentage: " + percentage);
                delayQueue.Enqueue(percentage);
                if (delayQueue.Count > _delay)
                {
                    float currentPercentage = delayQueue.Dequeue();
                    photoSensorField.SetBrightness((byte) (currentPercentage * 255));
                    Debug.Log("Brightness: " + currentPercentage);
                }
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                Debug.Log("Pressed F1 (Start Measure)");
                Measure();
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log("Pressed F2 (Measure next minute)");
                MeasureNextMin();
            }
            else if (Input.GetKeyDown(KeyCode.F3))
            {
                Debug.Log("Pressed F3 (Recenter Tracking)");
                InputTracking.Recenter();
            }
        }

        void FinishedTaskCallback()
        {
            Debug.Log("Finished Task. Go back to Idle State");
            state = State.Idle;
        }

        public void Measure()
        {
            if (state == State.Idle)
            {
                StartCoroutine(MeasureCoroutine());
            }
            else
            {
                Debug.LogWarning(
                    "Another task is already running. Wait till it' s finished before taking measurements");
            }
        }

        private IEnumerator MeasureCoroutine()
        {
            float startTime = Time.time + _countdown;
            while (Time.time < startTime)
            {
                float countdowon = startTime - Time.time;
                Debug.Log("Seconds till Start: " + countdowon);
                yield return null;
            }

            serial.SetNumberOfMeasurements(NR_OFF_MEASUREMENTS);
            state = State.Measure;
            SerialCommunication.FinishMeasurementCallback cb = FinishedMeasurementCallback;
            StartCoroutine(serial.AsyncMeasure(cb));
        }

        public void MeasureNextMin()
        {
            if (state == State.Idle)
            {
                serial.SetNumberOfMeasurements(NR_OFF_MEASUREMENTS);
                state = State.Measure;
                DateTime startTime = DateTime.UtcNow;
                startTime = startTime.AddMinutes(1);
                TimeSpan seconds = new TimeSpan(0, 0, startTime.Second);
                startTime = startTime.Subtract(seconds);
                SerialCommunication.FinishMeasurementCallback cb = FinishedMeasurementCallback;
                StartCoroutine(serial.AsyncMeasure(cb, startTime));
            }
        }

        private void FinishedMeasurementCallback(ArrayList returnArray)
        {
            Debug.Log("Finished Measurement");
            SensorData sensorData = new SensorData(returnArray);
            dataAnalyzer.GenerateOutputFiles(sensorData, _outputDirectory);
            state = State.Idle;
        }
    }
}