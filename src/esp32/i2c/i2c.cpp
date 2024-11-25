#include <Wire.h>
#include "./i2c.h"

// First bit signifies whether data is available in buffer for use
// Second bit signifies whether an error occured during reading
char i2c_flag = 0b00000000;
char *i2c_get_flag()
{
    return &i2c_flag;
}

char i2c_data_buffer[I2C_BUFFER_SIZE];
char *i2c_get_data_buffer()
{
    return i2c_data_buffer;
}

void i2c_init_listener()
{
    Wire.begin(ESP32_I2C_ADDRESS);
    Wire.onReceive(i2c_on_receive);
    Wire.onRequest(i2c_on_request);
}

void i2c_write(char *data, uint16_t length)
{
    Wire.beginTransmission(ARDUINO_I2C_ADDRESS);
    Wire.write(data, length);
    Wire.endTransmission();
}

void i2c_on_receive(uint16_t length)
{
    int i = 0;
    char data = 0;
    while (Wire.available() && i < I2C_BUFFER_SIZE)
    {
        data = Wire.read();
        i2c_data_buffer[i++] = data;
    }

    if (i > 0)
    {
        i2c_flag |= 0x01;
    }

    if (i != length)
    {
        i2c_flag |= 0x02;
    }
}

void i2c_on_request()
{
    Wire.write("ACK");
}
