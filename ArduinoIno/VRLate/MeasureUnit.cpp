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

#include "MeasureUnit.h"

// Use this to output debug info to console
#define DEBUG 1 
// Incerase this for more analog reads per measurement.
#define ANALOG_READ_AVERAGING 5
// Resolution of the AD-Converter
#define ANALOG_RESOLUTION 13

MeasureUnit::MeasureUnit() {
  pinMode(LED_MEASURING_PIN, OUTPUT);
  pinMode(SEN_POTENTIOMETER_PIN, INPUT);
  pinMode(SEN_PHOTODIODE_1_PIN, INPUT);
  pinMode(SEN_PHOTODIODE_2_PIN, INPUT);
  pinMode(SEN_PHOTODIODE_3_PIN, INPUT);
  pinMode(SEN_PHOTODIODE_4_PIN, INPUT);
  pinMode(SEN_MICROPHONE_PIN, INPUT);
  pinMode(BUZZER_PIN,OUTPUT);
  digitalWrite(LED_MEASURING_PIN, LOW);
  servo.attach(SERVO_PIN);
  servo.write(SERVO_START_POS);
  canceled = false;
  valuesInBuffer = 0;
  numberOfMeasureintervals = 5000;
  measureCounter = 0;
  measureInterval_us = 1000; // Default measure interval timespan
  analogReadResolution(ANALOG_RESOLUTION); // The Teensy 3.2 has a 13 bit resolution ADC
  analogReadAveraging(ANALOG_READ_AVERAGING); // Reduce Signal Noise
}

void MeasureUnit::begin() {
#ifdef DEBUG
  Serial.println(F("Begin measurement"));
#endif
  previousDirectionChange_ms = 0;
  servo.write(SERVO_END_POS);
  updateRotationPlatform();  
  valuesInBuffer = 0;
  measureCounter = 0;
  previousMeasure_us = micros();
  digitalWrite(LED_MEASURING_PIN, HIGH);  
  tone(BUZZER_PIN, BUZZER_FREQUENCY);
  unsigned long startMeasureTime = millis();
  audioDelay = -1;
  while(millis() - startMeasureTime < BUZZER_SOUND_LENGH_MS){
    // Check if Delay was Zero  
    int audioReading = digitalRead(SEN_MICROPHONE_PIN);
    if(audioDelay < 0 && audioReading == HIGH){
      audioDelay = millis() - startMeasureTime;
#ifdef DEBUG
  Serial.print(F("Set audio delay to: "));
  Serial.println(audioDelay);
#endif      
    }
  }
  noTone(BUZZER_PIN);
  measure();
}

void MeasureUnit::cancel() {
  finishMeasurement();
  canceled = true;  
}

void MeasureUnit::measure() {
  int arrLength = 5;
  uint16_t arr[arrLength];
  delayMicroseconds(10);
  arr[0] = analogRead(SEN_POTENTIOMETER_PIN);  
  delayMicroseconds(10);
  arr[1] = analogRead(SEN_PHOTODIODE_1_PIN);
  delayMicroseconds(10);
  arr[2] = analogRead(SEN_PHOTODIODE_2_PIN);
  delayMicroseconds(10);
  arr[3] = analogRead(SEN_PHOTODIODE_3_PIN);
  delayMicroseconds(10);
  arr[4] = analogRead(SEN_PHOTODIODE_4_PIN);
  pushOnBuffer(arr, arrLength);
  int audioReading = digitalRead(SEN_MICROPHONE_PIN);
  if(audioDelay < 0 && audioReading == HIGH){
    audioDelay = measureCounter + BUZZER_SOUND_LENGH_MS;
#ifdef DEBUG
  Serial.print(F("Set audio delay to: "));
  Serial.println(audioDelay);
#endif    
  }
  ++measureCounter;
}

void MeasureUnit::pushOnBuffer (const uint16_t *arr, int length) {
  if (valuesInBuffer + length <= BUFFER_SIZE) {
    for (int i = 0; i < length; i++) {
      buffer[valuesInBuffer] = arr[i];
      ++valuesInBuffer;
    }
  } else {
#ifdef DEBUG
    Serial.println(F("ERROR: Try to push values on buffer but it is full!"));
#endif
  }
}

bool MeasureUnit::update() {
  // Check if canceled
  if (canceled) {
#ifdef DEBUG
    Serial.println(F("User canceled measurement -> Go back to Idle state"));
#endif
    canceled = false;
    return false;
  }

  // Check if end of measurement
  if (measureCounter >= numberOfMeasureintervals) {
#ifdef DEBUG
    Serial.println(F("Finished Measurement -> Transmit data"));
#endif
    finishMeasurement();
    transmitData();
    return false;
  }
  
  // Check if need to measure
  unsigned long currentMicros = micros();
  if ((unsigned long)(currentMicros - previousMeasure_us) >= measureInterval_us) {
    measure();
    previousMeasure_us = currentMicros;
  }
    
  updateRotationPlatform();  
  return true;
}

// Send data to PC. 
// First Number is the audio delay
// Second is values in buffer. 
// Then (1 Poti, 4 Photo) * measure intervals
void MeasureUnit::transmitData() {
#ifdef DEBUG
  Serial.println(audioDelay);
#endif
  Serial2.println(audioDelay);  
#ifdef DEBUG
  Serial.println(valuesInBuffer);
#endif
  Serial2.println(valuesInBuffer);
  for (int i = 0; i < valuesInBuffer; ++i) {
#ifdef DEBUG
    Serial.println(buffer[i]);
#endif
    Serial2.println(buffer[i]);
  }
#ifdef DEBUG
  Serial.println(F("Finished transmission of measurement data to PC -> Go back to Idle state"));
#endif
}

void MeasureUnit::setNumberOfMeasureintervals(unsigned long n) {
  numberOfMeasureintervals = n;
#ifdef DEBUG
  Serial.print(F("Set Number of measure intervals to "));
  Serial.println(n);
#endif
}

// Cleanup and reset after finished measurement
void MeasureUnit::finishMeasurement(){    
    // Set servo to start pos
    servo.write(SERVO_START_POS);
    digitalWrite(LED_MEASURING_PIN, LOW);
}

// Check if need to rotate platform in other direction
void MeasureUnit::updateRotationPlatform(){
  unsigned long currentMillis = millis();
  if((unsigned long)(currentMillis - previousDirectionChange_ms) >= SERVO_CHANGE_DIRECTION_TIME_MS){
    int lastRotationDirection = servo.read();
    if(lastRotationDirection == SERVO_START_POS){
      servo.write(SERVO_END_POS);
    } else {
      servo.write(SERVO_START_POS);
    }
    previousDirectionChange_ms = currentMillis;
#ifdef DEBUG
    Serial.print(F("Changed direction. Last direction: "));
    Serial.println(lastRotationDirection);
#endif
  }  
}

