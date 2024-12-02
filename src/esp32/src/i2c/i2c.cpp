#include <Wire.h>
#include "./i2c.h"
#include "../storage/storage.h"
#include "../api_h/api.h"
#include <cstring>
#include "time.h"

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

void i2c_on_receive(int length)
{
    int i = 0;
    char data = 0;
    while (Wire.available() && i < I2C_BUFFER_SIZE)
    {
        data = Wire.read();
        i2c_data_buffer[i++] = data;
    }

    if (i == 1)
    {
        uint8_t control = i2c_data_buffer[0];
        if (control == 0xff)
        {
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
                memcpy(data + 1, (void *)(&prefs->tempTarget), 8);
                i2c_write(data, 9); // send tempTarget
            }
            else
            {
                double default_temp = 30;
                memcpy(data + 1, (void *)(&default_temp), 8);
                i2c_write(data, 9); // send tempTarget
            }

            data[0] = 1;
            if (prefs->tempTarget >= 3 && prefs->tempTarget <= 7)
            {
                memcpy(data + 1, (void *)(&prefs->phTarget), 8);
                i2c_write(data, 9); // send phTarget
            }
            else
            {
                double default_ph = 5;
                memcpy(data + 1, (void *)(&default_ph), 8);
                i2c_write(data, 9); // send tempTarget
            }

            data[0] = 2;
            if (prefs->tempTarget >= 500 && prefs->tempTarget <= 1300)
            {
                memcpy(data + 1, (void *)(&prefs->rpmTarget), 8);
                i2c_write(data, 9); // send rpmTarget
            }
            else
            {
                double default_rpm = 900;
                memcpy(data + 1, (void *)(&default_rpm), 8);
                i2c_write(data, 9); // send tempTarget
            }
        }
    }

    if (i == 9)
    {
        uint8_t dataType = i2c_data_buffer[0];
        double value = *((double *)i2c_data_buffer + 1);
        send_value_to_server(dataType, getTime(), value);
    }
}

void i2c_init_listener()
{
    Wire.begin(ESP32_I2C_ADDRESS);
    Wire.onReceive(i2c_on_receive);
}

void i2c_write(uint8_t *data, uint16_t length)
{
    if (!i2c_is_connected())
    {
        return;
    }

    Wire.beginTransmission(ARDUINO_I2C_ADDRESS);
    Wire.write(data, length);
    Wire.endTransmission();
}

void i2c_request(uint16_t length)
{
    Wire.requestFrom(ARDUINO_I2C_ADDRESS, length);
}
