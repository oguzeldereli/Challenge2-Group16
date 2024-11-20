#ifndef __I2C_H__
#define __I2C_H__

#include <cstdint>

#define I2C_BUFFER_SIZE 64

char *i2c_get_flag();
char *i2c_get_data_buffer();
void i2c_init_listener();
void i2c_write(char *data, uint16_t length);

#endif