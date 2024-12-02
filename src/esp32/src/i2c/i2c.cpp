#include <Wire.h>
#include "./i2c.h"
#include "../api_h/api.h"
#include "time.h"

uint8_t i2c_data_buffer[I2C_BUFFER_SIZE];
uint8_t *i2c_get_data_buffer()
{
    return i2c_data_buffer;
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

    if(i == 9)
    {
        uint8_t dataType = i2c_data_buffer[0];
        double value = *((double*)i2c_data_buffer + 1);
        send_value_to_server(dataType, time(nullptr), value);
    }
}

void i2c_init_listener()
{
    Wire.begin(ESP32_I2C_ADDRESS);
    Wire.onReceive(i2c_on_receive);
}

void i2c_write(uint8_t *data, uint16_t length)
{
    Wire.beginTransmission(ARDUINO_I2C_ADDRESS);
    Wire.write(data, length);
    Wire.endTransmission();
}

void i2c_request(uint16_t length)
{
    Wire.requestFrom(ARDUINO_I2C_ADDRESS, length);
}
