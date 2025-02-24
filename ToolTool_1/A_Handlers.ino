// If odd characters showed up.
void errorHandler()
{
  //nothing at the moment
}

void GGA_Handler() //Rec'd GGA
{
  // fix time
  parser.getArg(0, fixTime);
  // latitude
  parser.getArg(1, latitude);
  parser.getArg(2, latNS);
  // longitude
  parser.getArg(3, longitude);
  parser.getArg(4, lonEW);
  // fix quality
  parser.getArg(5, fixQuality);
  // satellite #
  parser.getArg(6, numSats);
  // HDOP
  parser.getArg(7, HDOP);
  // altitude
  parser.getArg(8, altitude);
  // Geoid Seperation sealevel
  parser.getArg(10, seperation);
  // time of last DGPS update
  parser.getArg(12, ageDGPS);

  GPSqualin1 = atoi(fixQuality);
  if (GPSqualin1 == 4)  GPSqualintime = millis() + 15000;
  if (GPSqualintime > millis() && (GPSqualin1 != 4)) {
    GPSqualin1 = 4;
    GPSqualistr = String(GPSqualin1);
    strglen = GPSqualistr.length() + 1;
    GPSqualistr.toCharArray(fixQuality, strglen);
  }
}  // end GGA_Handler()

// ##############################################

void VTG_Handler()
{
  // vtg heading
  parser.getArg(0, vtgHeading);
  // vtg Speed knots
  parser.getArg(4, speedKnots);
  // vtg Speed Km/h
  parser.getArg(6, speedKm);
}  // end VTG_Handler()

// ##############################################

void BuildNmea_GGA(void)
{
  strcpy(GGAnmea, "");
  strcat(GGAnmea, "$GNGGA,");
  strcat(GGAnmea, fixTime);
  strcat(GGAnmea, ",");
  strcat(GGAnmea, latitude);
  strcat(GGAnmea, ",");
  strcat(GGAnmea, latNS);
  strcat(GGAnmea, ",");
  strcat(GGAnmea, longitude);
  strcat(GGAnmea, ",");
  strcat(GGAnmea, lonEW);
  strcat(GGAnmea, ",");
  strcat(GGAnmea, fixQuality);
  strcat(GGAnmea, ",");
  strcat(GGAnmea, numSats);
  strcat(GGAnmea, ",");
  strcat(GGAnmea, HDOP);
  strcat(GGAnmea, ",");
  strcat(GGAnmea, altitude);
  strcat(GGAnmea, ",M,");
  strcat(GGAnmea, seperation);
  strcat(GGAnmea, ",M,");
  strcat(GGAnmea, ageDGPS);
  strcat(GGAnmea, ",,");
  strcat(GGAnmea, "*");
  GGA_NMEA_CalculateChecksum();
  strcat(GGAnmea, "\r");
} // end BuildNmea_GGA

//  ###################################################

void BuildNmea_VTG(void)
{
  strcpy(VTGnmea, "");
  strcat(VTGnmea, "$GNVTG,");
  strcat(VTGnmea, vtgHeading);
  strcat(VTGnmea, ",");
  strcat(VTGnmea, "T");
  strcat(VTGnmea, ",");
  strcat(VTGnmea, "");
  strcat(VTGnmea, ",");
  strcat(VTGnmea, "M");
  strcat(VTGnmea, ",");
  strcat(VTGnmea, speedKnots);
  strcat(VTGnmea, ",");
  strcat(VTGnmea, "N");
  strcat(VTGnmea, ",");
  strcat(VTGnmea, speedKm);
  strcat(VTGnmea, ",");
  strcat(VTGnmea, "K");
  strcat(VTGnmea, ",");
  strcat(VTGnmea, "A");
  strcat(VTGnmea, "*");
  VTGSatz_Korr = VTGnmea;
  VTG_NMEA_CalculateChecksum();
  strcat(VTGnmea, "\r");
  VTGSatz = VTGnmea;
}  // end BuildNmea_VTG

//  ###################################################

void GGA_NMEA_CalculateChecksum(void)
{
  int16_t sum = 0;
  int16_t inx = 0;
  char tmp;

  // The checksum calc starts after '$' and ends before '*'
  for (inx = 1; inx < 200; inx++)
  {
    tmp = GGAnmea[inx];

    // * Indicates end of data and start of checksum
    if (tmp == '*')
    {
      break;
    }

    sum ^= tmp;    // Build checksum
  }

  byte chk = (sum >> 4);
  char hex[2] = { asciiHex[chk], 0 };
  strcat(GGAnmea, hex);

  chk = (sum % 16);
  char hex2[2] = { asciiHex[chk], 0 };
  strcat(GGAnmea, hex2);
}  // end GGA_NMEA_CalculateChecksum

// ##############################################

void VTG_NMEA_CalculateChecksum(void)
{
  int16_t sum = 0;
  int16_t inx = 0;
  char tmp;

  // The checksum calc starts after '$' and ends before '*'
  for (inx = 1; inx < 200; inx++)
  {
    tmp = VTGnmea[inx];

    // * Indicates end of data and start of checksum
    if (tmp == '*')
    {
      break;
    }

    sum ^= tmp;    // Build checksum
  }

  byte chk = (sum >> 4);
  char hex[2] = { asciiHex[chk], 0 };
  strcat(VTGnmea, hex);

  chk = (sum % 16);
  char hex2[2] = { asciiHex[chk], 0 };
  strcat(VTGnmea, hex2);
}  // end VTG_NMEA_CalculateChecksum

// ##############################################
