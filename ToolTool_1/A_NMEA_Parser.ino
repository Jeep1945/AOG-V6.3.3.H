void Print_NMEA()
{
  BuildNmea_GGA();
  //Serial.println(GGAnmea);
  BuildNmea_VTG();
  //Serial.println(VTGnmea);

  if ((millis() - Single_begin_Time) > 90) {
    Single_begin_Time = millis();

 
       //Serial.println(GGAnmea);
       //Serial.println(VTGSatz);
       //Serial.println("");
       //Serial.println(latNS);
       //Serial.println(lonEW);


       ToolGGAnord = "";
       ToolGGAeast = "";
       ToolGGAnord.concat(latitude);
       ToolGGAeast.concat(longitude);


       unsigned long lati = (ToolGGAnord.toDouble()) * 100000;
       unsigned long longi = (ToolGGAeast.toDouble()) * 100000;
       Serial.println(ToolGGAnord);
       Serial.println(ToolGGAeast);

       PGN_215[5] = lati;// & 0xFF; // 0x78
       PGN_215[6] = (lati >> 8); // & 0xFF; // 0x56
       PGN_215[7] = (lati >> 16);// & 0xFF; // 0x34
       PGN_215[8] = (lati >> 24);// & 0xFF; // 0x12

       PGN_215[9] = longi;// & 0xFF; // 0x78
       PGN_215[10] = (longi >> 8);// & 0xFF; // 0x56
       PGN_215[11] = (longi >> 16);// & 0xFF; // 0x34
       PGN_215[12] = (longi >> 24);// & 0xFF; // 0x12

       //PGN_215[13] = (HimmelsNS);// & 0xFF; // 0x34
       //PGN_215[14] = (HimmelsWE);// & 0xFF; // 0x12

       send_Eth();

  
  }  // end   timed loop
} //  end Print_NMEA

//-----------------------------------------------------------
/*
 * 
 *    int len_GGA = strlen(GGAnmea);
    int len_VTG = strlen(VTGnmea);

    //Serial.println(GGAnmea);
    //Serial.println(VTGSatz);
    EthUDPToAOG.beginPacket(Eth_ipDestination, portDestination);
    EthUDPToAOG.write(GGAnmea, len_GGA);
    //Eth_udpPAOGI.write(VTGnmea, len_VTG);
    EthUDPToAOG.endPacket();



 */

  //-----------------------------------------------------------
  /*
           public byte[] pgn = new byte[] { 0x80, 0x81, 0x7f, 0xD9, 6, 0, 0, 0, 0, 0, 0, 0xCC };
            public int LowXTE = 5;
            public int HighXTE = 6;
            public int status = 7;
            public int LowXTEVeh = 8;
            public int HighXTEVeh = 9;
            public int VehRoll = 10;
*/
