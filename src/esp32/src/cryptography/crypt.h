#ifndef __CRYPT_H__
#define __CRYPT_H__

#include <cstdint>
#include <cstdlib>

uint8_t *sign_packet(uint8_t *key, uint8_t *bytes, size_t length);
bool validate_packet(uint8_t *key, uint8_t *bytes, size_t length, uint8_t *signature);
uint8_t *sha256_hash(uint8_t *message, size_t length);
void fill_random_array(void *buf, size_t len);

#endif