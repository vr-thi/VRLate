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
        private SensorData data;
        private string outputDirectory;
        private float[] trackingInputPercent;
        private float[] monitorOutputPercent;
        private int[] photosensorIntegerReadings;
        private int audioDelay;
        private MonitorType monitorType;
        private const string CSV_SEPARATOR = "\t";

        public int BlackLevelVoltage { get; set; }

        public void SetMonitorType(MonitorType monitorType)
        {
            this.monitorType = monitorType;
        }

        public void GenerateOutputFiles(SensorData data, string outputDirectory)
        {
            this.data = data;
            this.outputDirectory = outputDirectory;
            ConvertPhotosensorData();
            ConvertPotentiometerData();
            audioDelay = data.GetAudioDelay();
            WriteCSVFiles();
            Debug.Log("Finished generating output files");
        }

        public int[] GetPhotosensorIntegerReadings(SensorData data)
        {
            this.data = data;
            ConvertPhotosensorData();
            int[] photosensorIntegerReadingsClone = (int[])photosensorIntegerReadings.Clone();
            return photosensorIntegerReadingsClone;
        }

        private void ConvertPhotosensorData()
        {
            int[] photosensorData = data.GetPhotoSensorData();
            int nrOfMeasurements = photosensorData.Length;

            // TODO helper function
            if (monitorType == MonitorType.OLED_HMD)
            {
                photosensorIntegerReadings = new int[photosensorData.Length];
                for (int i = 0; i < photosensorIntegerReadings.Length; i++)
                {
                    int currentReading = photosensorData[i];
                    if (currentReading >= BlackLevelVoltage)
                    {
                        photosensorIntegerReadings[i] = currentReading;
                    }
                    else
                    {
                        photosensorIntegerReadings[i] = -1;
                    }
                }
            }
            else if (monitorType == MonitorType.LCD)
            {
                photosensorIntegerReadings = (int[])photosensorData.Clone();
            }
            else
            {
                throw new NotImplementedException("Display type not implemented yet.");
            }

            // Convert integer reading into percentage value
            monitorOutputPercent = new float[photosensorIntegerReadings.Length];
            for (int measurePos = 0; measurePos < photosensorIntegerReadings.Length; measurePos++)
            {
                int currIntegerReading = photosensorIntegerReadings[measurePos];
                if (currIntegerReading > 0)
                {
                    monitorOutputPercent[measurePos] = (float)currIntegerReading / VRLate.MICROCONTROLLER_MAX_AD_READING;
                }
                else
                {
                    monitorOutputPercent[measurePos] = -1f;
                }
            }

        }

        private void ConvertPotentiometerData()
        {
            int[] potentiometerData = data.GetPotentiometerData();
            trackingInputPercent = new float[potentiometerData.Length];
            for (int i = 0; i < potentiometerData.Length; i++)
            {
                int potValue = potentiometerData[i];
                float potDegrees = potValue * VRLate.POTENTIOMETER_MAX_ANGLE / VRLate.MICROCONTROLLER_MAX_AD_READING;
                float relValue = potDegrees / VRLate.MEASUREMENT_MAX_ANGLE;
                trackingInputPercent[i] = relValue;
            }
        }

        private void WriteCSVFiles()
        {
            if (!Directory.Exists(outputDirectory))
            {
                Debug.LogWarning("Output direcotry '" + outputDirectory + "' does not exist. Try to create one.");
                var newDir = Directory.CreateDirectory(outputDirectory);
            }
            int measureIntervals = trackingInputPercent.Length;
            string[] linesTrackingInput = new string[measureIntervals + 1];
            string[] linesMonitorOutput = new string[measureIntervals + 1];
            string[] lineAudioDelay = new string[1];

            // CSV Header
            linesTrackingInput[0] = "Interval" + CSV_SEPARATOR + "Value";
            linesMonitorOutput[0] = "Interval" + CSV_SEPARATOR + "Value";
            // Data
            for (int interval = 1; interval <= measureIntervals; interval++)
            {
                linesTrackingInput[interval] = interval + CSV_SEPARATOR + trackingInputPercent[interval - 1];
                linesMonitorOutput[interval] = interval + CSV_SEPARATOR + monitorOutputPercent[interval - 1];
            }
            lineAudioDelay[0] = audioDelay.ToString();

            File.WriteAllLines(outputDirectory + "/TrackingInput.csv", linesTrackingInput);
            File.WriteAllLines(outputDirectory + "/MonitorOutput.csv", linesMonitorOutput);
            File.WriteAllLines(outputDirectory + "/AudioDelay.txt", lineAudioDelay);
        }
    }
}