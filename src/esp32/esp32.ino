#include "src/connection/connection.h"
#include "src/i2c/i2c.h"
#include <HTTPClient.h>

// Replace with your eduroam credentials
char *ssid = "eduroam";
char *USER = "zcabogu@ucl.ac.uk";
char *PASS = "xxxxxxxxxxxx";

void setup()
{
    Serial.begin(115200); // initialize serial connection
    delay(1000);          // give time for serial to init
    Serial.println("Initializing I2C connection with Arduino...");
    i2c_init_listener(); // initialize arduino connection
    Serial.println("Done.");

    bool connected = false;
    do
    {
        Serial.println("Attempting WiFi connection...");
        connected = wifi_begin_enterprise(ssid, USER, PASS); // connect to eduroam
    } while (!connected);
    Serial.println("Done.");

    Serial.println("Configuring Time...");
    configTime(0, 0, "pool.ntp.org"); // configure time to UTC+0
    Serial.println("Done.");

    Serial.println("Initializing websocket connection with API...");
    websocket_begin(); // initialize api connection
    Serial.println("Done.");
}

void loop()
{
    websocket_keepalive();
}
