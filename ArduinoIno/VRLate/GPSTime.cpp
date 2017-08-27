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

#include "GPSTime.h"

#define DEBUG 1 // Use this to output debug info to console

static volatile gpsTime_t currTime;
static volatile uint32_t lastTimePulse_us = 0;
static volatile bool synced;

GPSTime::GPSTime() {
  Serial1.begin(9600);
  synced = false;
  pinMode(GPS_PPS_PIN, INPUT_PULLUP);
  pinMode(LED_FIXED_TO_GPS_PIN, OUTPUT);
  digitalWrite(LED_FIXED_TO_GPS_PIN, LOW);
  attachInterrupt(digitalPinToInterrupt(GPS_PPS_PIN), timepulseEvent, RISING);
#ifdef DEBUG
  Serial.println(F("GPSTime Initialized"));
#endif
}

void GPSTime::syncToGPSTime() {
  // critical section. Otherwise lastTimePulse_us might be changed during calculation and be greater than micros()!!!
  noInterrupts();
  uint32_t timePulseAge_us = (uint32_t)(micros() - lastTimePulse_us);
  interrupts();
  if (!synced) {
    smartdelay(100);
    decodeGPSTime();
  } else if (timePulseAge_us >= 1500000ul) {
#ifdef DEBUG
    Serial.println("Micros =" + micros());
    Serial.println("LastTP =" +lastTimePulse_us);
    Serial.print(F("Last Timepulse is too old: "));
    Serial.println(timePulseAge_us);
#endif
    updateSyncStatus(false);
  }
  smartdelay(100);
}

bool GPSTime::isSynced() {
  return synced;
}

// ISR for pps signal of GPS
void GPSTime::timepulseEvent() {
  uint32_t currentMicros = micros();
  if (synced) {
    // Check if Timepulse is older than 1.5 seconds
    if ((uint32_t)(currentMicros - lastTimePulse_us) >= 1500000ul) {
      updateSyncStatus(false);
    } else {
      incOneSecond();
    }
  }
  lastTimePulse_us = currentMicros;
}

void GPSTime::updateSyncStatus(bool newStat) {
  synced = newStat;
  if (synced) {
    digitalWrite(LED_FIXED_TO_GPS_PIN, HIGH);
  } else {
    digitalWrite(LED_FIXED_TO_GPS_PIN, LOW);
  }
#ifdef DEBUG
  Serial.print(F("Sync Status Changed to: "));
  if (synced) {
    Serial.println(F("Synced"));
  } else {
    Serial.println(F("NOT Synced"));
  }
#endif
}

void GPSTime::incOneSecond() {
  currTime.Second = (currTime.Second + 1) % 60;
  if (currTime.Second == 0) {
    currTime.Minute = (currTime.Minute + 1) % 60;
    if (currTime.Minute == 0) {
      currTime.Hour = (currTime.Hour + 1) % 24;
    }
  }
}

void GPSTime::decodeGPSTime() {
  int year;
  byte hour, minute, second, hundredths, month, day;
  unsigned long age_ms;
  gps.crack_datetime(&year, &month, &day, &hour, &minute, &second, &hundredths, &age_ms);
  if (validateTimestampForSync(hour, minute, second, age_ms)) {
    currTime.Hour = hour;
    currTime.Minute = minute;
    currTime.Second = second;
    updateSyncStatus(true);
  }
}

// Check if GPS-Timestamp can be used to sync to next Timepulse
bool GPSTime::validateTimestampForSync(byte hour, byte minute, byte second, unsigned long age_ms) {
  // critical section. Otherwise lastTimePulse_us might be changed during calculation and be greater than micros()!!!
  noInterrupts();
  uint32_t timePulseAge_us = (uint32_t)(micros() - lastTimePulse_us);
  interrupts();
  // Keep 5ms distance to Timepulse events
  uint32_t safetyTime_us = 5000;
  if (age_ms == TinyGPS::GPS_INVALID_AGE) {
#ifdef DEBUG
    Serial.println(F("No GPS Fix"));
#endif
    return false;
  }

#ifdef DEBUG
  Serial.print(F("New GPS Time Encoded: "));
  print_time(hour, minute, second, age_ms);
  Serial.print(F(" Last Timepulse in Microseconds: "));
  Serial.println(timePulseAge_us);
#endif

  if (timePulseAge_us <= (age_ms * 1000) + safetyTime_us) {
#ifdef DEBUG
    Serial.println(F("Timestamp might be older than the last Timpulse -> Wait for other timestamp"));
#endif
    return false;
  }  else if (timePulseAge_us >= 1000000 - safetyTime_us) {
#ifdef DEBUG
    Serial.println(F("Timestamp too close to next Timepulse -> Wait for other timestamp"));
#endif
    return false;
  } else {
#ifdef DEBUG
    Serial.println(F("Good GPS-Time. Sync to exact time with next Timepulse"));
#endif
    return true;
  }
}

void GPSTime::smartdelay(unsigned long ms)
{
  unsigned long start = millis();
  do
  {
    while (Serial1.available()) {
      gps.encode(Serial1.read());
    }
  } while (millis() - start < ms);
}

bool GPSTime::isEqual(const gpsTime_t &t) {
  return (currTime.Hour == t.Hour
          && currTime.Minute == t.Minute
          && currTime.Second == t.Second);
}

////// Debug functions //////

#ifdef DEBUG
void GPSTime::print_int(unsigned long val, unsigned long invalid, int len) {
  char sz[32];
  if (val == invalid)
    strcpy(sz, "*******");
  else
    sprintf(sz, "%ld", val);
  sz[len] = 0;
  for (int i = strlen(sz); i < len; ++i)
    sz[i] = ' ';
  if (len > 0)
    sz[len - 1] = ' ';
  Serial.print(sz);
  smartdelay(0);
}

void GPSTime::print_time(byte hour, byte minute, byte second, unsigned long age_ms) {
  char sz[32];
  Serial.print(F("GPS Time: "));
  sprintf(sz, " %02d:%02d:%02d", hour, minute, second);
  Serial.print(sz);
  Serial.print(F(" TimeAge: "));
  print_int(age_ms, TinyGPS::GPS_INVALID_AGE, 5);
  smartdelay(0);
}
#endif
