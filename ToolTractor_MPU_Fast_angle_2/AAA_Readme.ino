// Dualhead for AGopenGPS
// Private-use only! (you need to ask for a commercial-use)
// by Franz Husch  31.12.2020 in cooperation with Matthias Hammer
// and Valentin Ernst
// **** OTA is possible over Network  **** //
// Antennas cross to driveDirection on cabin symmetrical
// right Antenna is Rover (A) for position, left Antenna is MB (B) for heading
// Ntrip client for 7 Router or Handy hotspots or from AgopenGPS
// Progamm tries one time to connect,
// you can install a button from GND to PIN 4
// by pressing the button a new WiFi scan starts or Accesspoint for OTA.
// PIN 2 you can add an LED for ntrip is received, have a look to the Photo
// LED is on when no Network could be connected
// Dual_Antenna  0: for single antenna, 1: for Dualantenna
// send_amatron_nmea  to send GGA,VTG,GSA and ZDA to a second ESP32 0: for not 1: for sending,
// rollaktiv (0) for use roll in AgopenGPS, offset and hight of Antenna set in setup AOG
// rollaktiv (1) for use roll by ESP32, offset and hight of Antenna set to 0 in setup AOG
// WiFi_scan_Delay is the mount of sec, you will need start router or hotspot
// in Data Sourcess
// rollaktiv is 0, is done in AOG, so Antenna offset and hight is to do in AOG
// GGA_Send_Back_Time for SAPOS or Apos set to 10,
// by Fix "OGI", Heading GPS "Dual"
//  IMPORTANT  // serial USB baude changed to 38400
//  IMPORTANT  // you have to use the new AMA PVT config
// you get install 2 Ntrips and if you loose first, it changes automaticly to the second
// IMU_MPU6050 = 1;       // 0 no MPU   1: with MPU 
// move_line_buttons = 1;       // 0: no   1: by ethernet buttons to move AB line
// connect ethernet again with button pin 13
