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

using UnityEngine;
using UnityEngine.UI;

namespace VRLate
{
    /// <summary>
    /// Manage the bright area on the display to output the rotation as brightness code.
    /// </summary>
    public class Photosensorfield : MonoBehaviour
    {
        public Image PhotosensorBackground;
        public Image PhotosensorArea;

        private bool _displayGreenOnly = false;

        void Start()
        {
            // Let all images render in foreground (Effect Layer)
            PhotosensorBackground.material.renderQueue = 4000;
            PhotosensorArea.material.renderQueue = 4000;
        }

        /// <summary>
        /// Define the type of monitor used to take latency measurements.
        /// </summary>
        /// <param name="type">The used display technology</param>
        public void SetMonitorType(MonitorType type)
        {
            _displayGreenOnly = (type == MonitorType.DLP);
            if (type == MonitorType.OLED_HMD)
            {
                gameObject.transform.Rotate(new Vector3(180f, 180f, 0));
            }
        }

        /// <summary>
        /// Set the brightness of the output field.
        /// </summary>
        /// <param name="intensity">Intensity from 0 - 255.</param>
        public void SetBrightness(byte intensity)
        {
            byte intensityRedAndBlue = intensity;
            if (_displayGreenOnly)
            {
                intensityRedAndBlue = 0;
            }
            PhotosensorArea.color = new Color32(intensityRedAndBlue, intensity, intensityRedAndBlue, 255);
        }
    }
}
