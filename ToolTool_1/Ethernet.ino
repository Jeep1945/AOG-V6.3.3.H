bool set_local_or_listening_port(EthernetUDP* udp, const uint16_t port, char* context) {
  if ( ! udp->begin(port) ) {
    Serial.printf("E: udp->begin(%" PRIu16 ") - no sockets available", port);
    return false;
  }
  else {
    Serial.printf("I: udp->begin(%" PRIu16 ") results in local port %" PRIu16 " for %s\n", port, udp->localPort(), context);
    return true;
  }
}

void Eth_Start()
{
    delay(5000);
    Ethernet.init(Eth_CS_PIN);
    delay(200);
    Eth_myip[2] = Ethernet_3rd;
    Ethernet.begin(mac, Eth_myip);
    delay(200);
    // Check for Ethernet hardware present
    if (Ethernet.hardwareStatus() == EthernetNoHardware)
    {
        Serial.println("Ethernet shield was not found.  Sorry, can't run without hardware. :");
        Ethernet_running = false;
    }
    else
    {
        Serial.println("Ethernet hardware found, checking for connection");
        if (Ethernet.linkStatus() == LinkOFF)
        {
            Serial.println("Ethernet cable is not connected.");
            ESP.restart();
            Ethernet_running = false;
        }
        else
        {
            Serial.println("Ethernet status OK");
            Serial.print("Got IP ");
            Serial.println(Ethernet.localIP());
            if ((Ethernet.localIP()[0] == 0) && (Ethernet.localIP()[1] == 0) && (Ethernet.localIP()[2] == 0) && (Ethernet.localIP()[3] == 0))
            {
                // got IP 0.0.0.0 = no DCHP so use static IP
                Ethernet_running = true;
            }
            // use DHCP but change IP ending (x.x.x.80)
            if (!Ethernet_running)
            {
                for (byte n = 0; n < 3; n++)
                {
                    Eth_myip[n] = Ethernet.localIP()[n];
                    Eth_ipDestination[n] = Ethernet.localIP()[n];
                }
                Eth_ipDestination[3] = 255;
                Ethernet.setLocalIP(Eth_myip);
            }
            else
            { // use static IP
                for (byte n = 0; n < 3; n++)
                {
                    Eth_ipDestination[n] = Eth_myip[n];
                }
                Eth_ipDestination[3] = Eth_ipDest_ending;
                Ethernet.setLocalIP(Eth_myip);
            }

            Ethernet_running = true;
            Serial.print("Ethernet localIP:       ");
            Serial.println(Ethernet.localIP());
            Serial.print("Ethernet sending to IP: ");
            Serial.println(Eth_ipDestination);

            set_local_or_listening_port(&EthUDPToAOG,     49152,            "EthUDPToAOG");    // since we are only sending on this socket we choose a random local port
            set_local_or_listening_port(&EthUDPfromAOG,   AOG_listen_port,  "EthUDPfromAOG");
            set_local_or_listening_port(&EthUDPfromESP32, ToolntripPort,    "EthUDPfromESP32");
            
            /*
            // init UPD Port sending to AOG
            if (EthUDPToAOG.begin(ToolntripPort))
            {
                Serial.print("Ethernet UDP sending to tool: ");
                Serial.println(ToolntripPort);
            }

            // init UPD Port getting NTRIP from ESP32
            if (EthUDPfromESP32.begin(ToolntripPort)) // localPort
            {
                Serial.printf("EthUDPfromESP32.begin() on port : %" PRIu16 "\n", portMy);
                // Serial.println(ToolntripPort);
            }

            // init UPD Port sending to AOG
            if (EthUDPfromAOG.begin(localPort))
            {
                Serial.print("Ethernet UDP sending to tool: ");
                Serial.println(localPort);
            }
            */
        }
        //    Serial.println();
    }
}

//-------------------------------------------------------------------------------------------------

/*
  void read_Eth_DataFrom_AOG() {

  //read Data

  Serial.print("packetLength  ");
  Serial.println(packetLength);
  if (packetLength > 0) {
    EthUDPfromESP32.read(Eth_NTRIP_packetBuffer, packetLength);
    //    Serial.println(String(Eth_NTRIP_packetBuffer));
    for (int j = 0; j < packetLength; j++) {
      Serial2.write(Eth_NTRIP_packetBuffer[j]);
    }
  }
  }

  //-------------------------------------------------------------------------------------------------
  void read_Eth_Ntrip_ESP32() {

  //read Data

  Serial.print("packetLength  ");
  Serial.println(packetLength);
  if (packetLength > 0) {
    EthUDPfromESP32.read(Eth_NTRIP_packetBuffer, packetLength);
    //    Serial.println(String(Eth_NTRIP_packetBuffer));
    for (int j = 0; j < packetLength; j++) {
      Serial2.write(Eth_NTRIP_packetBuffer[j]);
    }
  }
  }
*/
//-------------------------------------------------------------------------------------------------

void send_Eth()
{

    int16_t CK_A = 0;
    for (uint8_t i = 2; i < PGN_215_Size; i++)
    {
        CK_A = (CK_A + PGN_215[i]);
    }
    PGN_215[PGN_215_Size] = CK_A;

    EthUDPToAOG.beginPacket(Eth_ipDestination, AOG_portDestination);
    EthUDPToAOG.write(PGN_215, sizeof(PGN_215));
    EthUDPToAOG.endPacket();

} // Hallo 1

//-------------------------------------------------------------------------------------------------//***************************************************************************************************************
