# VRLate
Tool to measure motion-to-photon and mouth-to-ear latency in distributed VR-Systems.

## Overview
Compared to existing measurement techniques, the described method is specially designed for distributed VR systems. It will help to quickly evaluate different setups in terms of motion-to-photon and mouth-to-ear latencies. The game-engine Unity 3D is used to simulate the virtual world. 
A quick overview of the used hardware and software of the measurement system will be given in the following two subsections.
<p>
  <img src="Images/systemOverview.png" width="300"/>
  <img src="Images/SystemImage.JPG" width="550"/>
</p>

### Motion-to-photon latency
Motion and the corresponding display output of a VR system are captured by a potentiometer and a photosensor which are both connected to a microcontroller. An object tracked by the VR system is mounted on the potentiometer and automatically rotated back and forth. The motion is controlled by a servo motor connected to the microcontroller. By monitoring the horizontal rotation with the potentiometer, it is possible to determine the rotary position of the real object at every interval.
To get the delay between the rotation of the real object and the corresponding display output, the latest angle captured by the motion-tracking system is presented on the VR display. 
The displayed value is converted into a brightness code so that it can be read with the microcontroller. By attaching the measurement unit’s photosensor to the display, it is possible to capture this brightness code and therefore the angle which was used by the VR system to render the current frame.
The perceived motion-to-photon latency is determined by comparing both signals with each other using cross-correlation.
### Mouth-to-ear latency
To determine the mouth-to-ear latency with the described measurement setup, a low-cost piezo buzzer and a microphone are used. Right before starting the measurement the microcontroller activates the buzzer for a short period of time. At every interval, the microcontroller checks if the attached microphone detects any sound. Then, the mouth-to-ear latency can be determined by counting the intervals between the beginning of the measurement and the received sound-impulse.
### Remote latency
If the delay between multiple VR systems needs to be measured, synchronization is required. In the presented method, GPS receivers are used to synchronize multiple microcontrollers and measure delays in distributed VR systems.

## Hardware

<img src="Images/hardwareSetup.png" width="600">

### Rotation platform
For the measurement setup, a potentiometer is used to continuously trace the rotation angle of the tracked object (e.g., an HMD). The platform rotary motion is controlled by a servo connected to the microcontroller. With this setup, steady back-and-forth movements of the tracked object are possible.
### Photosensor
The photosensor is used to monitor display outputs and captures the rotary angle of the tracking system which is output in a brightness code. During measurement, right before the rendering process, the VR system outputs the last captured horizontal rotation angle in an encoded brightness code. To get a resolution of 4096 different values, four areas on the display are used to output a number in the octal numeral system. By capturing each area with a light-sensitive sensor connected to the microcontroller, it is possible to get the rotation angle output of the VR system at every interval. The whole photosensor consists of four TSL250R photosensors. 

<img src="Images/photodiodes.png" width="300">

With the HMDs used in this work, it is not possible to directly attach the photosensor to the VR display without the need to disassemble the hardware. HMDs use optical lenses in front of the display system to expand the field of view. Without any light path correction, it is not possible to get a sharp image of the displayed frame on a flat surface.
To read the brightness code of the VR system on HMDs, an additional optical lens is necessary. For the measurement setup, convex lenses built into the low-cost VR glasses Google Cardboard where directly attached to the lenses of the HMD to correct the light path of the VR
display. Attaching the photosensor a few millimeters away from the correction lens makes it possible to read the displayed brightness code. 

## Software

### Prerequisites

To run the project the following software needs to be installed:

- **Arduino 1.6.13** IDE to compile the c++ code to the microcontroller. [link](https://www.arduino.cc/en/Main/OldSoftwareReleases "link")
- **Teensy Loader 1.32** Program used to allow the compilation of teensy code with the Arduino IDE. [link (windows)](https://www.pjrc.com/teensy/td_132/TeensyduinoInstall.exe)
- **Unity 3D 2017.1.1f1** Game engine [link](https://unity3d.com/de/get-unity/download/archive)
- **(optional) TTL Driver** Driver for used TTL adapter. Required to be installed manually for Windows 7.[link](https://www.jens-bretschneider.de/aktuelle-treiber-fur-seriell-zu-usb-adapter/)

> Note that you should install the versions that were tested. This is especially true for the Arduino IDE and the teensy loader where it is known for sure that a newer version won't compile the code as expected. With Arduino 1.8.x the size of the measurement units array leads to an unexpected error which stops the code from execution. This is a bug of the newer version and could not be resolved yet.

## Getting Started

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
