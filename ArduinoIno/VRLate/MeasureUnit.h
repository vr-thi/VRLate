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

#if !defined(MEASUREUNIT_H)
#define MEASUREUNIT_H

#include <Arduino.h>
#include <PWMServo.h>

#define BUFFER_SIZE 25002

const int SEN_POTENTIOMETER_PIN = A0;
const int SEN_PHOTODIODE_1_PIN = A1;
const int SEN_PHOTODIODE_2_PIN = A2;
const int SEN_PHOTODIODE_3_PIN = A3;
const int SEN_PHOTODIODE_4_PIN = A4;
const int SEN_MICROPHONE_PIN = 7;
const int BUZZER_PIN = 8;
const int SERVO_PIN = A6;
const unsigned long BUZZER_SOUND_LENGH_MS = 500;
const int BUZZER_FREQUENCY = 4000;
const byte LED_MEASURING_PIN = 4;
const int SERVO_CHANGE_DIRECTION_TIME_MS = 500;
const int SERVO_START_POS = 10;
const int SERVO_END_POS = 170;

class MeasureUnit {
  public:
    MeasureUnit();
    void begin(); 
    bool update(); // needs to be called in main loop during measurement. Return false if measurement is over
    void cancel();  // Cancel the current measure session
    void setNumberOfMeasureintervals(unsigned long nr);
  private:
    PWMServo servo;
    void measure();
    void finishMeasurement();
    void updateRotationPlatform();
    void pushOnBuffer (const uint16_t *arr, int length);
    void transmitData();
    uint16_t valuesInBuffer;
    uint16_t buffer[BUFFER_SIZE];
    uint16_t potiStartPos;
    bool canceled;
    unsigned long measureInterval_us;
    unsigned long numberOfMeasureintervals;
    unsigned long previousMeasure_us;
    unsigned long measureCounter;    
    unsigned long previousDirectionChange_ms;   
    long audioDelay;    
};
#endif
