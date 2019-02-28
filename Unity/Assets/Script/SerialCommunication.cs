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
    /// <summary>
    /// Communicate with microcontroller via a serial port. Send and receive messages.
    /// </summary>
    public class SerialCommunication : MonoBehaviour
    {
        // Callback is called in asyncMeasure after measurement finished
        public delegate void FinishMeasurementCallback(ArrayList retVal);

        private SerialAsyncReader _asyncReadJob = null;
        private SerialPort _stream;
        private string _port;
        private int _baudrate;

        /// <summary>
        /// Initializer serial communication.
        /// </summary>
        /// <param name="port">The port used to communicate with microcontroller.</param>
        /// <param name="baudrate">The baud rate for the communication (can be changed in the microcontroller code).</param>
        public void Setup(string port, int baudrate)
        {
            this._port = port;
            this._baudrate = baudrate;
            Open();
            CheckConnection();
        }

        /// <summary>
        /// Send SET_NO_MEASUREMENTS event to microcontoller.
        /// </summary>
        /// <param name="measurements">Number of measurements which shall be taken.</param>
        public void SetNumberOfMeasurements(int measurements)
        {
            if (CheckConnection())
            {
                string sendString = "SET_NO_MEASUREMENTS " + measurements;
                Debug.Log("Send " + sendString + " message to microcontroller");
                Write(sendString);
            }
        }

        /// <summary>
        /// Send MEASURE_AT command to microcontroller to start measurement at the exact next UTC timestamp received with the GPS-module.
        /// </summary>
        /// <param name="callback">Function which will be called when measurement is over.</param>
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
                    var startTimeArgument = startTime.Hour.ToString("D2") + " " + startTime.Minute.ToString("D2") +
                                            " " + startTime.Second.ToString("D2");
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

                _stream.ReadTimeout = 50;
                _asyncReadJob = new SerialAsyncReader();
                _asyncReadJob.InStream = _stream;
                _asyncReadJob.Start();
                while (!_asyncReadJob.Update())
                {
                    // Wait till job finished
                    yield return new WaitForSeconds(0.5f);
                }

                callback(_asyncReadJob.OutReturnData);
            }
            else
            {
                Debug.LogError("Connection to microcontroller was not possible");
            }
        }

        /// <summary>
        /// Check whether microncontroller is ready to start the next command.
        /// </summary>
        /// <returns>True if the next command can be send. False otherwise.</returns>
        private bool IsReady()
        {
            return true;
            /* TODO Check if Micro is at start pos. Right now no isReady mechanism is implemented on the client side.
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

        /// <summary>
        /// Check whether the client microcontroller responds to messages send via the serial connection.
        /// </summary>
        /// <returns>True if the client responds to a simple PING request.</returns>
        private bool CheckConnection()
        {
            if (!_stream.IsOpen)
            {
                Debug.LogError("Serial port could not be Opened (" + _port + "). Check Port number and restart!");
                return false;
            }

            Write("PING");
            var ret = Read(1000);
            if (ret != null && ret.Equals("PONG"))
            {
                Debug.Log("Communication with microcontroller was possible");
                return true;
            }

            // Otherwise the other side did not response
            Debug.LogError("No microcontroller respond! Check wiring, port and baud rate!");
            return false;
        }

        /// <summary>
        /// Low level Serial Function. Open connection.
        /// </summary>
        private void Open()
        {
            Debug.Log("Open serial Port: " + _port + " baud rate: " + _baudrate);
            _stream = new SerialPort(_port, _baudrate, Parity.None, 8, StopBits.One);
            _stream.Open();
        }

        /// <summary>
        /// Write message via stream.
        /// </summary>
        private void Write(string message)
        {
            _stream.WriteLine(message);
            _stream.BaseStream.Flush();
        }

        /// <summary>
        /// Read serial connection stream.
        /// </summary>
        private string Read(int timeout_ms = 0)
        {
            _stream.ReadTimeout = timeout_ms;
            try
            {
                return _stream.ReadLine();
            }
            catch (TimeoutException)
            {
                return null;
            }
        }

        /// <summary>
        /// Close serial connection stream
        /// </summary>
        private void Close()
        {
            _stream.Close();
        }
    }
}