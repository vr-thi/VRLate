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
using System.IO;
using UnityEngine;

namespace VRLate
{
    public class DataAnalyzer
    {
        public int BlackLevelVoltage { get; set; }

        private const string CSV_SEPARATOR = "\t";

        private SensorData _data;
        private string _outputDirectory;
        private float[] _trackingInputPercent;
        private float[] _monitorOutputPercent;
        private int[] _photosensorIntegerReadings;
        private int _audioDelay;
        private MonitorType _monitorType;

        public void SetMonitorType(MonitorType monitorType)
        {
            this._monitorType = monitorType;
        }

        public void GenerateOutputFiles(SensorData data, string outputDirectory)
        {
            this._data = data;
            this._outputDirectory = outputDirectory;
            ConvertPhotosensorData();
            ConvertPotentiometerData();
            _audioDelay = data.GetAudioDelay();
            WriteCSVFiles();
            Debug.Log("Finished generating output files");
        }

        public int[] GetPhotosensorIntegerReadings(SensorData data)
        {
            this._data = data;
            ConvertPhotosensorData();
            int[] photosensorIntegerReadingsClone = (int[]) _photosensorIntegerReadings.Clone();
            return photosensorIntegerReadingsClone;
        }

        private void ConvertPhotosensorData()
        {
            int[] photosensorData = _data.GetPhotoSensorData();

            // TODO helper function
            if (_monitorType == MonitorType.OLED_HMD)
            {
                _photosensorIntegerReadings = new int[photosensorData.Length];
                for (int i = 0; i < _photosensorIntegerReadings.Length; i++)
                {
                    int currentReading = photosensorData[i];
                    if (currentReading >= BlackLevelVoltage)
                    {
                        _photosensorIntegerReadings[i] = currentReading;
                    }
                    else
                    {
                        _photosensorIntegerReadings[i] = -1;
                    }
                }
            }
            else if (_monitorType == MonitorType.LCD)
            {
                _photosensorIntegerReadings = (int[]) photosensorData.Clone();
            }
            else
            {
                throw new NotImplementedException("Display type not implemented yet.");
            }

            // Convert integer reading into percentage value
            _monitorOutputPercent = new float[_photosensorIntegerReadings.Length];
            for (int measurePos = 0; measurePos < _photosensorIntegerReadings.Length; measurePos++)
            {
                int currIntegerReading = _photosensorIntegerReadings[measurePos];
                if (currIntegerReading > 0)
                {
                    _monitorOutputPercent[measurePos] =
                        (float) currIntegerReading / VRLate.MICROCONTROLLER_MAX_AD_READING;
                }
                else
                {
                    _monitorOutputPercent[measurePos] = -1f;
                }
            }
        }

        private void ConvertPotentiometerData()
        {
            int[] potentiometerData = _data.GetPotentiometerData();
            _trackingInputPercent = new float[potentiometerData.Length];
            for (int i = 0; i < potentiometerData.Length; i++)
            {
                int potValue = potentiometerData[i];
                float potDegrees = potValue * VRLate.POTENTIOMETER_MAX_ANGLE / VRLate.MICROCONTROLLER_MAX_AD_READING;
                float relValue = potDegrees / VRLate.MEASUREMENT_MAX_ANGLE;
                _trackingInputPercent[i] = relValue;
            }
        }

        private void WriteCSVFiles()
        {
            if (!Directory.Exists(_outputDirectory))
            {
                Debug.LogWarning(string.Format("Output directory '{0}' does not exist. Try to create one.", _outputDirectory));
                var newDir = Directory.CreateDirectory(_outputDirectory);
            }

            int measureIntervals = _trackingInputPercent.Length;
            string[] linesTrackingInput = new string[measureIntervals + 1];
            string[] linesMonitorOutput = new string[measureIntervals + 1];
            string[] lineAudioDelay = new string[1];

            // CSV Header
            const string headerText = "Interval" + CSV_SEPARATOR + "Value";
            linesTrackingInput[0] = headerText;
            linesMonitorOutput[0] = headerText;

            // Data
            for (var interval = 1; interval <= measureIntervals; interval++)
            {
                linesTrackingInput[interval] = interval + CSV_SEPARATOR + _trackingInputPercent[interval - 1];
                linesMonitorOutput[interval] = interval + CSV_SEPARATOR + _monitorOutputPercent[interval - 1];
            }

            lineAudioDelay[0] = _audioDelay.ToString();

            File.WriteAllLines(_outputDirectory + "/TrackingInput.csv", linesTrackingInput);
            File.WriteAllLines(_outputDirectory + "/MonitorOutput.csv", linesMonitorOutput);
            File.WriteAllLines(_outputDirectory + "/AudioDelay.txt", lineAudioDelay);
        }
    }
}