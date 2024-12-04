#include <Wire.h>
#include "./i2c.h"
#include "../storage/storage.h"
#include "../api_h/api.h"
#include <cstring>
#include "time.h"
#include <Arduino.h>

uint8_t i2c_data_buffer[I2C_BUFFER_SIZE];
uint8_t *i2c_get_data_buffer()
{
    return i2c_data_buffer;
}

bool is_connection_established = false;
bool i2c_is_connected()
{
    return is_connection_established;
}

void i2c_set_is_connected(bool set)
{
    is_connection_established = set;
}

void i2c_init_listener()
{
    Wire.begin();
    Wire.setTimeout(1000); 
    while(i2c_is_connected() == false)
    {
        Serial.println("Requesting ACK from Arduino...");
        int len = i2c_request(1); // request 1 byte 0xff to see if connection is established
        handle_response(len);
    }
}

void i2c_write(uint8_t *data, uint16_t length)
{
    if (!i2c_is_connected())
    {
        return;
    }

    Wire.beginTransmission(SLAVE_ADDRESS);
    Wire.write(data, length);
    Wire.endTransmission();
}

void handle_response(int i)
{
    if (i == 1)
    {
        uint8_t control = i2c_data_buffer[0];
        if (control == 0xff)
        {
            Serial.println("Received ff, ackking connection");
            i2c_set_is_connected(true);
            uint8_t ack[3] = {'A', 'C', 'K'};
            i2c_write(ack, 3); // ack the connection
            set_status(1);
            send_status_to_server(getTime());

            preferences_t *prefs = get_preferences();
            uint8_t data[9];
            memset(data, 0, 9);

            data[0] = 0;
            if (prefs->tempTarget >= 25 && prefs->tempTarget <= 35)
            {
                Serial.print("Sending temp: ");
                Serial.println(prefs->tempTarget);
                memcpy(data + 1, (void *)(&prefs->tempTarget), 8);
                i2c_write(data, 9); // send tempTarget
            }
            else
            {
                Serial.print("Sending temp: ");
                double default_temp = 30;
                Serial.println(default_temp);
                memcpy(data + 1, (void *)(&default_temp), 8);
                i2c_write(data, 9); // send tempTarget
            }

            data[0] = 1;
            if (prefs->tempTarget >= 3 && prefs->tempTarget <= 7)
            {
                Serial.print("Sending ph: ");
                Serial.println(prefs->phTarget);
                memcpy(data + 1, (void *)(&prefs->phTarget), 8);
                i2c_write(data, 9); // send phTarget
            }
            else
            {
                Serial.print("Sending ph: ");
                double default_ph = 5;
                Serial.println(default_ph);
                memcpy(data + 1, (void *)(&default_ph), 8);
                i2c_write(data, 9); // send phTarget
            }

            data[0] = 2;
            if (prefs->tempTarget >= 500 && prefs->tempTarget <= 1300)
            {
                Serial.print("Sending rpm: ");
                Serial.println(prefs->rpmTarget);
                memcpy(data + 1, (void *)(&prefs->rpmTarget), 8);
                i2c_write(data, 9); // send rpmTarget
            }
            else
            {
                Serial.print("Sending rpm: ");
                double default_rpm = 900;
                Serial.println(default_rpm);
                memcpy(data + 1, (void *)(&default_rpm), 8);
                i2c_write(data, 9); // send rpmTarget
            }
        }
    }

    if (i == 9)
    {
        uint8_t dataType = i2c_data_buffer[0];
        double value = *((double *)i2c_data_buffer + 1);
        Serial.print("Received value ");
        if(dataType == 0)
            Serial.print("temp: ");
        if(dataType == 1)
            Serial.print("ph: ");
        if(dataType == 2)
            Serial.print("rpm: ");
        Serial.println(value);
        send_value_to_server(dataType, getTime(), value);
    }
}

int i2c_request(uint16_t length)
{
    Wire.requestFrom(SLAVE_ADDRESS, length);
    int i = 0;
    char data = 0;
    while (Wire.available() && i < I2C_BUFFER_SIZE && i < length)
    {
        data = Wire.read();
        i2c_data_buffer[i++] = data;
    }

    Serial.print("Received data of length");
    Serial.println(i);

    return i;
}
