
String VERS = "Version ToolTool_2.2 04.02.2025";
//
// AAA_Readme for instructions

//  ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//  +++++++++++++++++++++++++++++++  BEGIN Setup +++++++++++++++++++++++++++++++++++++++
//  ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

byte Eth_myip[4] = {192, 168, 1, 122}; // IP address to send UDP data to
// byte Eth_myip[4] = { 192, 168, 1, 125 };  //IP address to send UDP data to
// byte Eth_myip[4] = { 10, 0, 0, 124 };//IP address to send UDP data to
byte Ethernet_3rd = 1; //{ 192, 168,     [1]   , 125 };//3rd nummer of IP address to send UDP data to

int Antennahight = 290;        // roll is in Position calculated,
byte Eth_CS_PIN = 5;           // CS PIN with SPI Ethernet hardware W 5500  SPI config: MOSI 23 / MISO 19 / CLK18 / CS5, GND, 3.3V
int IMU_MPU6050 = 1;           // 1: to 10   1: from Dual   10: from MPU
int IMU_MPU6050_direction = 2; // Drivedirection  Y=1:  -Y=2:  X=3:  -X=4:
int Roll_Dual_MPU = 1;         // from 1 to 10 1: from Dual   10: from MPU
int move_line_buttons = 0;     // 0: no   1: buttons to move AB line
int Headingfilter = 1;         // 1: no   10: most filter
int Rollfilter = 5;            // 1: no   10: most filter

//  ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
//  ++++++++++++++++++++++++++++++++  END Setup  +++++++++++++++++++++++++++++++++++++++
//  ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

/* ++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
    +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ */
// libraries -------------------------------
#include <Wire.h>
#include <math.h>
#include "z_NTRIPClient.h"
#include <Ethernet.h>
#include <EthernetUdp.h>
#include <ESPmDNS.h>
#include "zNMEAParser.h"
#include "mpu6050_FastAngles.h"

// connection plan:
//  ESP32--- Right F9P GPS pos --- Left F9P Heading-----Sentences
//   RX1----------TX1--------------------------------UBX-Nav-PVT out   (=position+speed)
//   TX1----------RX1--------------------------------RTCM in           (NTRIP comming from AOG to get absolute/correct postion
//   RX2--------------------------------TX1----------UBX-RelPosNED out (=position relative to other Antenna)
//   TX2--------------------------------RX1----------
//                RX2-------------------TX2----------RTCM 1005+1074+1084+1094+1124+1230 (activate in PVT F9P!! = NTRIP for relative positioning)

// IO pins ----------------------------------------------------------------------------------------
#define RX0 3 // USB
#define TX0 1 // USB

#define RX1 27 // simpleRTK TX1 1. Antenna = NMEA
#define TX1 16

#define RX2 25 // simpleRTK RX2(TX2) 1. Antenna = Relposned
#define TX2 17 // simpleRTK TX2(RX2) 1. Antenna = RTCM 3.3

#define Button_left 12
#define Button_middle 14
#define Button_right 26
#define Ethernet_reset 13
#define SerialNmea Serial1

// loop time variables in microseconds
long lastTime = 0;
byte XOR;
char c;
char b;
String t;
bool Ethernet_need = false, Ethernet_need_AOG = false;
String Conn = " all right";
int carrSoln;
bool gnssFixOk, diffSoln, relPosValid;

// Ntrip WiFi
byte WiFi_netw_nr = 0;
byte WiFi_netw[8] = {0, 0, 0, 0, 0, 0, 0, 0};
String RTCM_Packet;
unsigned long Single_begin_Time = millis();
unsigned long lastTime_heading = millis(), MPU_Time = millis();
unsigned long Button_delay40 = millis(), Button_delay41 = millis(), Button_delay42 = millis();
double nordWinkel_old, eastWinkel_old;
int ntrip_from_AgopenGPS = 0, ntrip_attempt = 0;
bool network_found = false;
int buttonState = 0, buttonState_Eth;
byte IPadress[4] = {0, 0, 0, 0};
int net_found = 0, Ntriphotspot_an = 0, WiFi_an = 0;
#define Button_ReScan 4 // pin 4  if button pressed, WiFi scan is starting

// Ethernet
// byte Eth_myip[4] = { 192, 168, 1, 124 };//IP address to send UDP data to
// byte Eth_myip[4] = { 10, 0, 0, 124 };//IP address to send UDP data to
// byte mac[] = {0x90, 0xA2, 0xDA, 0x10, 0xB3, 0x1B}; // original

// Bernhard: ToolTraktor has 0x9A in the end. changed to 0x9F
// byte mac[] = { 0x2C, 0xF7, 0xF1, 0x08, 0x00, 0x9F };
byte mac[] = {0x90, 0xA2, 0xDA, 0x10, 0xB3, 0x1B}; // original

byte Eth_ipDest_ending = 100;        // ending of IP address to send UDP data to router

// +++++ UDP comm ports
uint16_t ToolntripPort = 5122;       // port NTRIP data from AOG comes in
uint16_t portMy = 5124;          // this is port of this module: Autosteer = 5577 IMU = 5566 GPS =
uint16_t AOG_portDestination = 9999; // Port of AOG that listens
uint16_t AOG_listen_port = 8888;
//unsigned int localPort = 8888;       // local port to listen for UDP packets
// +++++ UDP comm ports


bool Ethernet_running = false, send_IP_back = false;
uint8_t Eth_NTRIP_packetBuffer[1500]; // buffer for receiving and sending data
char    Eth_AOG_packetBuffer[512];   // buffer for receiving and sending data
byte    ReplyBufferAGIO[180] = "";   // a string to send back

int m;
unsigned int packetLength;
unsigned long send_IP_back_time = millis();

IPAddress Eth_ipDestination;
IPAddress ipDestination1;
IPAddress src_ip;

// settings pgn
byte PGN_215[] = {0x80, 0x81, 0x7f, 0xD7, 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xCC};
int8_t PGN_215_Size = sizeof(PGN_215) - 1;
uint8_t PGN_233[] = {0x80, 0x81, 0x7f, 0xE9, 9, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0xCC};
int8_t PGN_233_Size = sizeof(PGN_233) - 1;

// STEER
byte ReplyBufferToolData[200];
double ToolXTE;
double ToolLookAhead;
int guidanceStatus;
double XTEVeh;
double VehRoll;

// An EthernetUDP instance to let us send and receive packets over UDP
EthernetUDP EthUDPToAOG;
EthernetUDP EthUDPfromAOG;
EthernetUDP EthUDPfromESP32;


// Heading
float heading, heading1, heading2, headingUBX, headingzuvor = 0;
String heading_str, heading_ubx_str;
char heading_ch[20], heading_UBX_ch[20];
double headingUBXmin, headingUBXmax, headingVTGmin, headingVTGmax;
double speeed = 0;

// roll
float roll, rollCorrectionDistance = 0.00, GGDs, GGDs1, GGDs2, GRDs, GRDs1, GRDs2;
String roll_str;
char roll_ch[8];
double rollnord = 0.0, rolleast = 0.0;
double rollnord1 = 0.0, rolleast1 = 0.0;
double rollnord_before = 0.0, rolleast_before = 0.0;
double relPosD, relPosDH;
double rollzuvor = 0;
double PI180 = 57.295791;
double hzuvor[10], hzuvormin, hzuvormax, heading_MPU6050_zuvor[10];
double Rzuvor[10];
double headingcorrectur = 0, rollcorrectur = 0;
double baseline, baseline1, baselineHorizontal;
double fixnorddeci, fixeastdeci;         // coordinates in decimalen
double fixnorddeci_old, fixeastdeci_old; // coordinates in decimalen
double rollnordabs, rollnordrel, rolleastabs, rolleastrel;

byte CK_A = 0, CK_B = 0;
byte incoming_char;

// NMEA erstellen
byte inByte;
byte inByte_UBX;
String Button_left_S, Button_middle_S, Button_right_S;
String MPU_Heading_S, MPU_Roll_S, MPU_Yaw_S, MPU6050_Data;
int start = 0, strglen;
String nmea = "", ToolGGAnord = "", ToolGGAeast = "";
String GGAnord = "", GGAeast = "", GGAWestEast = "", GGANordSued = "";
String GPSqualistr = "", WEcoordinaten, NScoordinaten;
String GGASatz = "", VTGSatz = "", GGASatz_Korr, VTGSatz_Korr = "", GGASatz_send_back = "";
int GPSqualin1 = 0, GPSqualinzuvor = 1, GPSqualintime = 1, GGA_check = 0;
String ZDASatz = "";
int i = 0, j = 0, ij = 0, iji = 0;
double nordWinkel, eastWinkel;
double fixnorddeci_before = 0.0000, fixeastdeci_before = 0.0000;
byte PositionBuffer[80];

/* A parser is declared with 3 handlers at most */
NMEAParser<3> parser;

// Conversion to Hexidecimal
const char *asciiHex = "0123456789ABCDEF";

// the new PAOGI sentence buffer
char GGAnmea[90];
char VTGnmea[60];
char PAOGI_Msg[150];

// GGA
char fixTime[12];
char latitude[15];
char latNS[3];
char longitude[15];
char lonEW[3];
char fixQuality[2];
char numSats[4];
char HDOP[5];
char altitude[12];
char seperation[8];
char ageDGPS[10];

char NScoordinaten_ch[15];
char WEcoordinaten_ch[15];

// VTG
char vtgHeading[12] = {};
char speedKnots[10] = {};
char speedKm[10] = {};

byte UDPVTGMsg[40], UDPGGAMsg[90];
byte ReplyBufferVTG[40] = "", ReplyBufferGGA[90] = ""; // a string to send back

// Chechsum controll
String checksum = "";

// MPU6050
String RollHeading = "";
double heading_MPU6050_hzuvormin = 360;
double heading_MPU6050_hzuvormax = 0, heading_MPU6050_corridor;
int move_ABline = 0, heading_source = 0;
float roll_MPU6050 = 0, heading_MPU6050 = 0, roll1, roll2;
float roll_MPU6050_diff = 0, heading_MPU6050_diff = 0, heading_MPU6050_offset = 0, heading_MPU6050_drift = 0;
// int Button_left = 0, Button_middle = 0, Button_right = 0;
int ABline_Direction[3] = {0, 0, 0}; // {heading, cm of movement, same direction as AB line}
bool MPU6050_received = false;
mpu6050_FastAngles mpu;
double ZCorrect = 0, headingbefore;
unsigned long lastReadTime = 0;
const unsigned long readInterval = 90; // Intervalo de leitura em milissegundos
float angleX = 0;
float angleY = 0;
float angleZ = 0;
int buttonState_left, buttonState_middle, buttonState_right;

union UBXMessage
{
  struct
  {
    unsigned char HeaderA;
    unsigned char HeaderB;
    unsigned char cls;
    unsigned char id;
    unsigned short len;
    unsigned char reserved;
    unsigned char reserved1;
    unsigned short refStationId;
    unsigned long iTOW;
    long relPosN;
    long relPosE;
    long relPosD;
    long relPosLength;
    long relPosHeading;
    unsigned long reserved2;
    char relPosHPN;
    char relPosHPE;
    char relPosHPD;
    char relPosHPLength;
    unsigned long accN;
    unsigned long accE;
    unsigned long accD;
    unsigned long accLength;
    unsigned long accHeading;
    unsigned long reserved3;
    unsigned long flags;
  } relposned;
  byte rawBuffer[72];
} ubxmessage;

// bool debugmode = true;  // GGA,VTG,
bool debugmode = false;

// ================================================================
// ===                      INITIAL SETUP                       ===
// ================================================================

void setup()
{
  ubxmessage.rawBuffer[0] = 0xB5;
  ubxmessage.rawBuffer[1] = 0x62;
  ubxmessage.rawBuffer[2] = 0x01;
  ubxmessage.rawBuffer[3] = 0x3C;

  delay(10);
  Serial.begin(38400);
  delay(10);
  Serial.println("");
  Serial.println(VERS);
  Serial.println("");
  Serial.println("Start setup");
  Serial.println("");

  SerialNmea.begin(115200, SERIAL_8N1, RX1, TX1);
  delay(10);
  Serial2.begin(115200, SERIAL_8N1, RX2, TX2);
  delay(10);
  pinMode(Button_ReScan, INPUT_PULLUP);
  // pinMode(LED_ntrip_ON, OUTPUT);
  // digitalWrite(LED_ntrip_ON, HIGH);
  pinMode(Button_left, INPUT_PULLUP);
  pinMode(Button_middle, INPUT_PULLUP);
  pinMode(Button_right, INPUT_PULLUP);
  pinMode(Ethernet_reset, OUTPUT);

  // Prevent W5500 to get into Reset Loop Bug
  digitalWrite(Ethernet_reset, LOW); // Reset Ethernet Modul
  delay(300);
  digitalWrite(Ethernet_reset, HIGH); // Start Ethernet Modul

  delay(100);
  Serial.print(" Antennahight : ");
  Serial.println(Antennahight); // roll is in Position calculated, in AgOpenGPS mit 0 cm
  Serial.print(" IMU_MPU6050 : ");
  Serial.println(IMU_MPU6050); // 0: no   1: to dualheading to stabilize heading under trees
  Serial.print(" IMU_MPU6050_direction : ");
  Serial.println(IMU_MPU6050_direction); // Drivedirection  Y=1:  -Y=2:  X=3:  -X=4:
  Serial.print(" Roll_Dual_MPU : ");
  Serial.println(Roll_Dual_MPU); // from 1 to 10 1: from Dual   10: from MPU
  Serial.print(" move_line_buttons : ");
  Serial.println(move_line_buttons); // 0: no   1: buttons to move AB line
  Serial.print(" Headingfilter : ");
  Serial.println(Headingfilter); // 1: no   10: most filter
  Serial.print(" Rollfilter : ");
  Serial.println(Rollfilter); // 1: no   10: most filter

  Serial.println("Start Ethernet");
  Eth_Start();

  // the dash means wildcard
  parser.setErrorHandler(errorHandler);
  parser.addHandler("G-GGA", GGA_Handler);
  parser.addHandler("G-VTG", VTG_Handler);

  // initialize device
  if (IMU_MPU6050 > 1)
  {

    Headingfilter = constrain(Headingfilter, 1, 8);
    Roll_Dual_MPU = constrain(Roll_Dual_MPU, 1, 8);
    if (Roll_Dual_MPU != 1)
    {
      Serial.println("");
      Serial.print("IMU_MPU6050  : ");
      Serial.println(IMU_MPU6050);
    }
    Serial.println("");
    Serial.println("");
    Serial.println("");
    Serial.println(VERS);
    Serial.println("");
    Serial.println("End setup");
    Serial.println("");
  }
}

  size_t read_udp_into_buffer(UDP & udp, uint8_t buffer[], size_t buffer_len) {
    int packetsize = udp.parsePacket();

    if (packetsize <= 0) {
      return 0;
    }

    size_t bytes_to_read;
    if ( packetsize > buffer_len ) {
      bytes_to_read = buffer_len;
    }
    else {
      bytes_to_read = packetsize;
    }

    size_t bytes_read = udp.readBytes(buffer, bytes_to_read);

    return bytes_read;
  }

  size_t copy_udp(UDP & udp, Print & target, uint8_t buffer[], size_t buffer_len)
  {
    const size_t bytes_read = read_udp_into_buffer(udp, buffer, buffer_len);
    if ( bytes_read == 0 ) {
      return 0;
    }

    const size_t written = target.write(buffer, bytes_read);
    return written;
  }


  void to_hex_string(uint8_t buf[], size_t len, char *dest)
  {
    const char *hex = "0123456789ABCDEF";
    for (int i = 0; i < len; i++)
    {
      *dest++ = hex[(buf[i] >> 4) & 0xF];
      *dest++ = hex[(buf[i]) & 0xF];
    }
    *dest = '\0';
  }

  uint8_t tmpBuffer[2048];

  // ================================================================
  // ===                    MAIN PROGRAM LOOP                     ===
  // ================================================================

  void loop()
  {
    //
    // read_Steer Data from AOG()
    //
    size_t AOG_data_len = read_udp_into_buffer(EthUDPfromAOG, ReplyBufferToolData, sizeof(ReplyBufferToolData));
    if (AOG_data_len > 0)
    {
      // test from command line:
      //  printf '\x80\x81\x7F\xe9spindi' | nc -u -w 1 192.168.1.122 8888
      udpToolSteerRec(ReplyBufferToolData, AOG_data_len);
    }

    //
    // forward RTCM data: from UDP to Serial
    //
    size_t written_rtcm = copy_udp(EthUDPfromESP32, Serial2, tmpBuffer, sizeof(tmpBuffer));
    if (written_rtcm > 0)
    {
      //char hex[128];
      //to_hex_string(tmpBuffer, 32, hex);
      Serial.printf("copy_udp: %4zu bytes\n", written_rtcm);
    }

    // read Date from MPU6050, and/or from 3 buttons to move the line
    if (move_line_buttons == 1)
      button_linemove();

    //  read NMEA msg from F9P (PVT) and pars them in NMEA_read()   ##############################################
    while (SerialNmea.available())
    { // If anything comes in SerialNmea
      parser << SerialNmea.read();
    }

    //  read UBX msg from F9P (heading)    ######################################################################
    if (Serial2.available())
    { // If anything comes in Serial2
      Serial.println("Serial2");
      incoming_char = Serial2.read(); // ESP32 read RELPOSNED from F9P
      if (i < 4 && incoming_char == ubxmessage.rawBuffer[i])
      {
        i++;
      }
      else if (i > 3)
      {
        ubxmessage.rawBuffer[i] = incoming_char;
        i++;
      }
    }
    if (i > 71)
    {
      CK_A = 0;
      CK_B = 0;
      for (i = 2; i < 70; i++)
      {
        CK_A = CK_A + ubxmessage.rawBuffer[i];
        CK_B = CK_B + CK_A;
      }

      if (CK_A == ubxmessage.rawBuffer[70] && CK_B == ubxmessage.rawBuffer[71])
      {

        if (IMU_MPU6050 == 1)
          Heading_MPU6050V(); // to correct the drift

        Print_NMEA();
        // Serial.println("ubxmessage");

        // Serial.println("ACK Checksum Failure: ");
      }
      i = 0;
    }

    // Spindler - ausgebaut
    // Print_NMEA();

    // Serial.println("Print_NMEA");

    //  LED on == no WiFi or Ethernet, off == WiFi or Ethernet   #########################################
    /*
      if (Ethernet_running) {
        if ((millis() - WiFi_blink_Time) < 500) {
          digitalWrite(LED_ntrip_ON, HIGH);
        } else {
          digitalWrite(LED_ntrip_ON, LOW);
          WiFi_blink_Time = millis();
        }
      } else {
        //digitalWrite(LED_ntrip_ON, HIGH);
      }
    */
    if (guidanceStatus == 1)
      ToolSteering();
    if (IMU_MPU6050 == 1)
      RollHeading_MPU();

  } // end main loop

  //------------------------------------------------------------------------------------------

  // callback when received packets
  void udpToolSteerRec(uint8_t AOG_reply[], size_t len)
  {
    /* IPAddress src(src_ip[0],src_ip[1],src_ip[2],src_ip[3]);
      Serial.print("dPort:");  Serial.print(dest_port);
      Serial.print("  sPort: ");  Serial.print(src_port);
      Serial.print("  sIP: ");  ether.printIp(src_ip);  Serial.println("  end");

      //for (int16_t i = 0; i < len; i++) {
      //Serial.print(udpData[i],HEX); Serial.print("\t"); } Serial.println(len);
    */

    if (AOG_reply[0] == 0x80 && AOG_reply[1] == 0x81 && AOG_reply[2] == 0x7F) // Data
    {
      if (AOG_reply[3] == 0xE9) // 233
      {
        ToolXTE = ((float)(AOG_reply[5] | AOG_reply[6] << 8)) * 0.1; // Tool too left minus,  in cm

        ToolLookAhead = ((float)(AOG_reply[7] | AOG_reply[8] << 8)); // in cm

        guidanceStatus = (float)AOG_reply[9]; // Tool steer activ = 1, 0 = inactiv

        XTEVeh = ((float)(AOG_reply[10] | AOG_reply[11] << 8)); //  free

        VehRoll = ((float)(AOG_reply[12] | AOG_reply[13] << 8)) * 0.1; // roll of tractor in degrees

        Serial.print("ToolXTE  ");
        Serial.println(ToolXTE);
        Serial.print("ToolLookAhead  ");
        Serial.println(ToolLookAhead);
        Serial.print("guidanceStatus  ");
        Serial.println(guidanceStatus);
        Serial.print("XTEVeh  ");
        Serial.println(XTEVeh);
        Serial.print("VehRoll  ");
        Serial.println(VehRoll);
      }
    }
  }
