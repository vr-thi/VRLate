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
using System.IO.Ports;
using UnityEngine;

namespace VRLate
{
    public class SerialCommunication : MonoBehaviour
    {
        // Callback is called in asyncMeasure after measurement finished
        public delegate void FinishMeasurementCallback(ArrayList retVal);

        private SerialAsyncReader asyncReadJob = null;
        private SerialPort stream;
        private string port;
        private int baudrate;

        public void Setup(string port, int baudrate)
        {
            this.port = port;
            this.baudrate = baudrate;
            Open();
            CheckConnection();
        }

        public void SetNumberOfMeasurements(int nr)
        {
            if (CheckConnection())
            {
                string sendString = "SET_NO_MEASUREMENTS " + nr;
                Debug.Log("Send " + sendString + " message to microcontroller");
                Write(sendString);
            }
        }

        public IEnumerator AsyncMeasure(FinishMeasurementCallback callback)
        {
            yield return AsyncMeasure(callback, DateTime.UtcNow);
        }

        public IEnumerator AsyncMeasure(FinishMeasurementCallback callback, DateTime startTime)
        {
            if (CheckConnection() && IsReady())
            {
                if (startTime.CompareTo(DateTime.UtcNow) > 0)
                {
                    string startTimeArgument = startTime.Hour.ToString("D2") + " " + startTime.Minute.ToString("D2") + " " + startTime.Second.ToString("D2");
                    Debug.Log("Send 'MEASURE_AT " + startTimeArgument + "' to microcontroller");
                    Write("MEASURE_AT " + startTimeArgument);
                    //Wait till start of measurement
                    while (startTime.CompareTo(DateTime.UtcNow) > 0)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }
                }
                else
                {
                    Debug.Log("Start measuring directly. Send MEASURE message to microcontroller");
                    Write("MEASURE");
                }
                stream.ReadTimeout = 50;
                asyncReadJob = new SerialAsyncReader();
                asyncReadJob.InStream = stream;
                asyncReadJob.Start();
                while (!asyncReadJob.Update())
                {
                    // Wait till job finished
                    yield return new WaitForSeconds(0.5f);
                }
                callback(asyncReadJob.OutReturnData);
            }
            else
            {
                Debug.LogError("Connection to microcontroller was not possible");
            }
        }

        /// Private Functions /// 

        private bool IsReady()
        {
            return true;
            /* TODO Check if Micro is at start pos
		Write ("READY");
		string ret = Read (100);
		if (ret != null && ret.Equals ("READY")) {
			Debug.Log ("Microcontroller is at startposition and ready to start measureing");
			return true;
		} else {
			Debug.LogError ("Microcontroller is not Ready yet. Maybe not at startposition!");
			return false;
		}*/
        }

        private bool CheckConnection()
        {
            if (!stream.IsOpen)
            {
                Debug.LogError("Serial port could not be Opened (" + port + "). Check Port number and restart!");
                return false;
            }
            else
            {
                Write("PING");
                string ret = Read(1000);
                if (ret != null && ret.Equals("PONG"))
                {
                    Debug.Log("Communication with michrocontroller was possible");
                    return true;
                }
                else
                {
                    Debug.LogError("No microcontroller respond! Check wiring, port and baudrate!");
                    return false;
                }
            }
        }

        /// Low level Serial Functions /// 

        private void Open()
        {
            Debug.Log("Open serial Port: " + port + " Baudrate: " + baudrate);
            stream = new SerialPort(port, baudrate, Parity.None, 8, StopBits.One);
            stream.Open();
        }

        private void Write(string message)
        {
            stream.WriteLine(message);
            stream.BaseStream.Flush();
        }

        private string Read(int timeout_ms = 0)
        {
            stream.ReadTimeout = timeout_ms;
            try
            {
                return stream.ReadLine();
            }
            catch (TimeoutException)
            {
                return null;
            }
        }

        private void Close()
        {
            stream.Close();
        }
    }
}