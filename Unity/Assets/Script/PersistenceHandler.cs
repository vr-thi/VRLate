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
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace VRLate
{
	public static class PersistenceHandler
	{
	
		public static void SavePhotosensorCalibration (PhotosensorCalibration config)
		{
			BinaryFormatter bf = new BinaryFormatter ();
			FileStream file = File.Create (Application.persistentDataPath + "/photosensorCalibration.gd");
			bf.Serialize (file, config);
			file.Close ();
		}

		public static PhotosensorCalibration LoadPhotosensorCalibration ()
		{
			if (File.Exists (Application.persistentDataPath + "/photosensorCalibration.gd")) {
				BinaryFormatter bf = new BinaryFormatter ();
				FileStream file = File.Open (Application.persistentDataPath + "/photosensorCalibration.gd", FileMode.Open);
				PhotosensorCalibration cc = (PhotosensorCalibration)bf.Deserialize (file);
				Debug.Log ("Found calibration file. Created: "
				+ cc.getCreationDateTime ().Day + "."
				+ cc.getCreationDateTime ().Month + "."
				+ cc.getCreationDateTime ().Year + " Time: "
				+ cc.getCreationDateTime ().Hour + ":"
				+ cc.getCreationDateTime ().Minute + ":"
				+ cc.getCreationDateTime ().Second);
				file.Close (); 
				return cc;
			} else {
				Debug.LogWarning ("PhotosensorCalibration file not found. Calibrate before taking measurements");
				return null;
			}			
		}
	}
}