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

#if !defined(GPSTIME_H)
#define GPSTIME_H

#include <TinyGPS.h>

const int GPS_PPS_PIN = 2;
const int LED_FIXED_TO_GPS_PIN = 3;

typedef struct  {
  uint8_t Second;
  uint8_t Minute;
  uint8_t Hour;
}   gpsTime_t;


class GPSTime {
  public:
    GPSTime();
    bool isSynced();
    void syncToGPSTime();
    static bool isEqual(const gpsTime_t &t);
  private:
    TinyGPS gps;
    static void updateSyncStatus(bool newStat);
    static void incOneSecond();
    static void timepulseEvent();
    bool validateTimestampForSync(byte hour, byte minute, byte second,unsigned long age_ms);
    void decodeGPSTime();
    void smartdelay(unsigned long ms);
    void print_time(byte hour, byte minute, byte second,unsigned long timeAge);
    void print_int(unsigned long val, unsigned long invalid, int len);
};

#endif

