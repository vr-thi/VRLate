/******************************************************************************* 
TODO License
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

