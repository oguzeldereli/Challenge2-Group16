#ifndef __STORAGE_H__
#define __STORAGE_H__

#include <cstdint>

void store_bytes(const char *key, void *bytes, size_t length);
uint8_t *get_read_buffer();
uint8_t *read_bytes(const char *key);
void remove_key(const char *key);
void clear_storage();

typedef struct
{
    char identifier[32];
    char secret[32];
    char signatureKey[32];
} preferences_t;

void set_preferences(preferences_t *prefs);
preferences_t *get_preferences();

#endif