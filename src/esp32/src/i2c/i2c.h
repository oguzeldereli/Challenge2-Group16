#ifndef __I2C_H__
#define __I2C_H__

#include <cstdint>

#define I2C_BUFFER_SIZE 64
#define SLAVE_ADDRESS 0x08

bool i2c_is_connected();
void i2c_set_is_connected(bool set);
uint8_t *i2c_get_data_buffer();
void i2c_init_listener();
void i2c_write(uint8_t *data, uint16_t length);
void handle_response(int i);
int i2c_request(uint16_t length);

#endif