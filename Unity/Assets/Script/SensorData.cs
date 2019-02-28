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
    public class SensorData
    {
        private ArrayList _rawData;
        // First Index is the sensor ID. Second the received value counter
        private int[] _photoSensorData;
        private int[] _potentiometerData;
        private int _audioDelay;

        public SensorData(ArrayList data)
        {
            _rawData = data;
            ParseRawData();
        }

        public void PrintRawData()
        {
            foreach (var item in _rawData)
            {
                Debug.Log(_rawData);
            }
        }

        public int[] GetPhotoSensorData()
        {
            return _photoSensorData;
        }

        public int[] GetPotentiometerData()
        {
            return _potentiometerData;
        }

        public int GetAudioDelay()
        {
            return _audioDelay;
        }

        private void ParseRawData()
        {
            if (_rawData == null)
            {
                Debug.LogError("No Data received. Please check connection to Microcontroller");
                return;
            }
            _audioDelay = (int)_rawData[0];
            int nrOfSensorReadings = (_rawData.Count - 1) / 5;
            Debug.Log("Number of Sensor readings: " + nrOfSensorReadings);
            _photoSensorData = new int[nrOfSensorReadings];
            _potentiometerData = new int[nrOfSensorReadings];

            for (int i = 0; i < nrOfSensorReadings; ++i)
            {
                _potentiometerData[i] = (int)_rawData[5 * i + 1];
                _photoSensorData[i] = (int)_rawData[5 * i + 2];
            }
        }

        // Hide default ctor
        private SensorData()
        {
        }
    }
}