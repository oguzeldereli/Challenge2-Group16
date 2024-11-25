#include "src/connection/connection.h"
#include "src/i2c/i2c.h"

// Replace with your eduroam credentials
char* ssid = "eduroam";
char* USER = "zcabogu@ucl.ac.uk";
char* PASS = "***REMOVED***";

void setup() {
    Serial.begin(115200); // initialize serial connection
    i2c_init_listener(); // initialize arduino connection

    bool connected = false;
    do 
    {
        Serial.println("Attempting WiFi connection...");
        connected = wifi_begin_enterprise(ssid, USER, PASS); // connect to eduroam
    }
    while(!connected);

    configTime(0, 0, "pool.ntp.org"); // configure time to UTC+0

    websocket_begin(); // initialize api connection
}

void loop() 
{

}
