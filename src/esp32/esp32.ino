#include "src/connection/connection.h"
#include "src/i2c/i2c.h"
#include "src/api_h/api.h"
#include <HTTPClient.h>

// Replace with your eduroam credentials
char *ssid = "eduroam";
char *USER = "zcabogu@ucl.ac.uk";
char *PASS = "Muhtesem3011";

void setup()
{
    Serial.begin(115200); // initialize serial connection
    delay(1000);          // give time for serial to init
    Serial.println("Initializing I2C connection with Arduino...");
    // i2c_init_listener();
    Serial.println("Done.");

    bool connected = true;
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

unsigned long previousMillis = 0;
void loop()
{
    websocket_keepalive();
    /*
    unsigned long currentMillis = millis();
    if (currentMillis - previousMillis >= 1000 && i2c_is_connected()) 
    {
        previousMillis = currentMillis;
        
        //uint8_t command = 0x00;
        //i2c_write(&command, 1);
        //int len =  i2c_request(5); // constantly temp data
        //handle_response(len);
        //send_value_to_server(0, getTime(), 30.3, 0);

        //command = 0x01;
        //i2c_write(&command, 1);
        //len =  i2c_request(5); // constantly ph data
        //handle_response(len);
        //send_value_to_server(1, getTime(), 3.1, 0);

        //command = 0x02;
        //i2c_write(&command, 1);
        //len =  i2c_request(5); // constantly rpm data
        //handle_response(len);
        //send_value_to_server(2, getTime(), 1000.41, 0);
        Serial.println("sent");
    }
    */
}
