#include "src/connection/connection.h"

// Replace with your eduroam credentials
char* ssid = "eduroam";
char* USER = "zcabogu@ucl.ac.uk";
char* PASS = "Muhtesem3011";

void setup() {
  Serial.begin(115200);
  bool connected = wifi_begin_enterprise(ssid, USER, PASS);
  if (connected) {
    Serial.println("Hello WiFi");
  }
}

void loop() {
}
