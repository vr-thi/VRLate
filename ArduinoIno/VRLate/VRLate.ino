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

#include <TinyGPS.h>
#include <PWMServo.h>
#include "GPSTime.h"
#include "MeasureUnit.h"
#include "Serial2Command.h"

// Use this to output debug info to console
#define DEBUG 1 

const int STATUS_LED_PIN = 13;
enum State { IDLE, MEASURE, WAIT_FOR_TIMESTAMP};
State state = IDLE;
GPSTime *gpsTime;
MeasureUnit *measureUnit;
gpsTime_t startMeasureTimestamp;
SerialCommand serialCommand;

void setup()
{
#ifdef DEBUG
  Serial.begin(250000);
  delay(3000); // Wait a bit to see the debuging messages in Cons
  Serial.println(F("Start Setup"));
#endif
  // Enable Serial2 used for PC-Communication
  Serial2.begin(250000);
  addSerialCommands();
  gpsTime = new GPSTime();
  measureUnit = new MeasureUnit();
#ifdef DEBUG
  Serial.println(F("Finished Setup"));
#endif
  pinMode(STATUS_LED_PIN, OUTPUT);
  digitalWrite(STATUS_LED_PIN, HIGH);
}

void loop()
{
  switch (state) {
    case IDLE:
      gpsTime->syncToGPSTime();
      break;
    case WAIT_FOR_TIMESTAMP:
      waitForGPSTime(startMeasureTimestamp);
      break;
    case MEASURE:
      if (!measureUnit->update()) {
        state = IDLE;
      }
      break;
  }
  serialCommand.readSerial();
}

void waitForGPSTime(gpsTime_t &startMeasureTimestamp) {
  if (gpsTime->isSynced()) {
    if (gpsTime->isEqual(startMeasureTimestamp)) {
      measureUnit->begin();
      state = MEASURE;
    }
  } else {
#ifdef DEBUG
    Serial.println(F("Could not start measurement. GPS is not synced to Timepulse Signal -> Go back to Idle state"));
#endif
    state = IDLE;
  }
}

//// Serial Communication ////

// Connect the command with the executed function
void addSerialCommands() {
  serialCommand.addCommand("PING", cmd_ping);
  //serialCommand.addCommand("READY", cmd_ready); TODO!!! Check if ready to start measurement
  serialCommand.addCommand("MEASURE", cmd_measure);
  serialCommand.addCommand("MEASURE_AT", cmd_measureAt);
  serialCommand.addCommand("CANCEL", cmd_cancel);
  serialCommand.addCommand("SET_NO_MEASUREMENTS", cmd_numberMeasureintervals);
  serialCommand.addDefaultHandler(cmd_unknown);
}

// Ignore unkown commands
void cmd_unknown() {
#ifdef DEBUG
  Serial.println(F("WARNING: Received unknown command"));
#endif
}

// Answer to ping request with PONG
void cmd_ping() {
  Serial2.println(F("PONG"));
#ifdef DEBUG
  Serial.println(F("Received PING Command"));
#endif
}

// Expects one parameter. Number of intervals
void cmd_numberMeasureintervals() {
#ifdef DEBUG
  Serial.println(F("Received SET_NO_MEASUREMENTS Command"));
#endif
  char *arg = serialCommand.next();
  if (arg != NULL) {
    unsigned long n = atol(arg);  // Converts a char string to long
    measureUnit->setNumberOfMeasureintervals(n);
  } else {
#ifdef DEBUG
    Serial.println(F("ERROR: No time argument given. Expected measuretime in us as first argument"));
#endif
  }
}

// Cancel if currently measuring
void cmd_cancel() {
#ifdef DEBUG
  Serial.println(F("Received CANCEL Command"));
#endif
  if (state == MEASURE) {
    measureUnit->cancel();
  } else {
#ifdef DEBUG
    Serial.println(F("WARNING: Ignore cancel because not measuring right now"));
#endif
  }
}

// Start measureing without waiting for specific timecode
void cmd_measure() {
#ifdef DEBUG
  Serial.println(F("Received MEASURE message"));
#endif
  measureUnit->begin();
  state = MEASURE;
}

// Three arguments expected. Timestamp as UTC. MEASURE_AT HH MM SS
void cmd_measureAt() {
#ifdef DEBUG
  Serial.println(F("Received MEASURE_AT message"));
#endif
  if (parseTimeArguments()) {
    state = WAIT_FOR_TIMESTAMP;
  } else {
#ifdef DEBUG
    Serial.println(F("ERROR Could not parse Time Arguments. Expected UTC-Timestamp: HH MM SS"));
#endif
  }
}

bool parseTimeArguments() {
  char *arg;
  int h, m, s;

  arg = serialCommand.next();
  if (arg != NULL) {
    h = atoi(arg);  // Converts a char string to an integer
  } else {
    return false;
  }
  arg = serialCommand.next();
  if (arg != NULL) {
    m = atol(arg);
  } else {
    return false;
  }
  arg = serialCommand.next();
  if (arg != NULL) {
    s = atol(arg);
  } else {
    return false;
  }

  if (h >= 0 && h < 24
      && m >= 0 && m < 60
      && s >= 0 && s < 60) {
#ifdef DEBUG
    Serial.print(F("Parsed time arguments. Next measure time: "));
    char sz[32];
    sprintf(sz, " %02d:%02d:%02d", h, m, s);
    Serial.println(sz);
#endif
    startMeasureTimestamp.Hour = h;
    startMeasureTimestamp.Minute = m;
    startMeasureTimestamp.Second = s;
    return true;
  } else {
#ifdef DEBUG
    Serial.println(F("No Valid Timestamp"));
#endif
    return false;
  }
}
