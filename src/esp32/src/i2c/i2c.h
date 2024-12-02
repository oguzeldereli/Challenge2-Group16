#ifndef __I2C_H__
#define __I2C_H__

#include <cstdint>

#define I2C_BUFFER_SIZE 64
#define ARDUINO_I2C_ADDRESS 0x09
#define ESP32_I2C_ADDRESS 0x08

uint8_t *i2c_get_data_buffer();
void i2c_init_listener();
void i2c_write(uint8_t *data, uint16_t length);

#endif