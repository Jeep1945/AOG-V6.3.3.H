void rollcalc()
{
  double nord = 11119494.9494,  east = 0, nordWinkel, eastWinkel;
  String norddeci, eastdeci, nordGrad , eastGrad;
  float rollCorrectionDistance;
  Paogi_true_UBX = true;
  rollzuvor = roll;
  if (AntDistance != 0)  baseline = AntDistance;
  roll = asin(relPosD / baseline) * PI180;
  if (debugmode) {
    Serial.print("  relPosD   :" + String(relPosD));
    Serial.print("  baseline   :" + String(baseline));
  }
  roll *= -1; // 100 left tilt should be negative
  roll =  constrain(roll, -30, 30);
  roll1 = roll;

  if (debugmode) {
    Serial.print("  roll : ");
    Serial.print(roll);
    //Serial.print("  roll1 :" + String(roll));
  }

  Rollfilter = constrain(Rollfilter, 1, 8);
  if (Rollfilter > 1) {
    for (j = 1; j <= 9; j++) {
      Rzuvor[j - 1] = Rzuvor[j];
      if (debugmode)   Serial.print(" R: " + String(j) + ", " + String(Rzuvor[j - 1]));
    }
    Rzuvor[9] = roll;
    GRDs = 0;
    for (j = 0; j <= 9; j++) {
      GRDs += Rzuvor[j] * (j + 1);
    }
    GRDs /= 55;
    roll = GRDs;
  }

  if ((Roll_MPU6050 == 1) && (Roll_Dual_MPU > 0)) {
    Roll_MPU6050V();
    if (abs(roll) < 0.1) {
      roll_MPU6050_diff = -roll_MPU6050;
    }
    roll2 = roll_MPU6050 + roll_MPU6050_diff;
    roll = roll * (1 - Roll_Dual_MPU * 0.1) + roll2 * (Roll_Dual_MPU * 0.1);

  }
  if (debugmode) {
    Serial.print("  GRDs : ");
    Serial.print(GRDs);
    Serial.print("  roll : ");
    Serial.print(roll);
    Serial.print("  roll1 : ");
    Serial.print(roll1);
    Serial.print("  roll2 : ");
    Serial.println(roll2);
  }

  rollCorrectionDistance = sin((roll) / PI180) * tractorhight;  // roll deviation
  baselineHorizontal = (cos((roll) / PI180) * baseline + 2 * (ABline_Direction[1] * ABline_Direction[2]));
  rollnordabs = (sin(heading  / PI180) * rollCorrectionDistance);
  rollnordabs += (sin(heading  / PI180) * (0.5 * baselineHorizontal));
  rolleastabs = (cos(heading  / PI180) * -rollCorrectionDistance);
  rolleastabs -= (cos(heading  / PI180) * (0.5 * baselineHorizontal));
  // 1° auf Erdkugel == 11119494,9494 cm
  // bei Nord nord = 11119494,9494 cm  /1°
  // bei East east = (cos(nord(heading)) * nord
  GGAnord = "";
  GGAeast = "";
  GGAnord.concat(latitude);
  GGAeast.concat(longitude);
  norddeci = (GGAnord.substring(2)); //Daten von GGAnord
  nordGrad = (GGAnord.substring(0, 2));
  eastdeci = (GGAeast.substring(3)); //Daten von GGAeast
  eastGrad = (GGAeast.substring(0, 3));
  fixnorddeci = norddeci.toDouble();
  nordWinkel = nordGrad.toDouble();
  fixeastdeci = eastdeci.toDouble();
  eastWinkel = eastGrad.toDouble();
  fixnorddeci = fixnorddeci / 60.0;
  fixeastdeci = fixeastdeci / 60.0;
  double nordWinkel1 = nordWinkel + fixnorddeci;
  // bei Nord nord = 11119494,9494 cm  /1°
  rollnordrel = rollnordabs / nord + fixnorddeci;
  // bei East east = (cos(nord())
  east = cos(nordWinkel1 / PI180) * 637100080;  // the radius of earth by north
  east = (2 * east * 3.141592) / 360;    // cm pro grad
  if (GGAWestEast == "W")  rolleastabs *= -1;
  rolleastrel = rolleastabs / east + fixeastdeci;
  rollnordrel = rollnordrel * 60.0;
  rolleastrel = rolleastrel * 60.0;
  rollnord1 = (nordWinkel * 100 + rollnordrel);
  rolleast1 = (eastWinkel * 100 + rolleastrel);

  Coordinaten_check();

  if (debugmode)  {
    Serial.println("");
    //    Serial.print("  fixnorddeci   :" + String(fixnorddeci, 7));
    //    Serial.print("  fixeastdeci   :" + String(fixeastdeci, 7));
    Serial.print(" rollnordrel   :" + String(rollnordrel, 7));
    Serial.println(" rolleastrel   :" + String(rolleastrel, 7));
    Serial.print  ("   rollnord1  " + String(rollnord1, 7));
    Serial.println("   rolleast1  " + String(rolleast1, 7));
    /*    Serial.print("  relPosD 2  :" + String(relPosD));
        Serial.print("  relPosDH   :" + String(relPosDH));
        Serial.print("  rollCorrectionDistance  :" + String(rollCorrectionDistance));
        Serial.print("  roll       :" + String(roll));
        Serial.print("  rollzuvor  :" + String(rollzuvor));
        Serial.println("  roll  :" + String(roll));
    */
  }
} // Ende
