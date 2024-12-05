#ifndef __STORAGE_H__
#define __STORAGE_H__

#include <cstdint>
#include <cstdlib>

void store_bytes(const char *key, void *bytes, size_t length);
uint8_t *get_read_buffer();
uint8_t *read_bytes(const char *key);
void remove_key(const char *key);
void clear_storage();

typedef struct
{
    uint8_t identifier[32];
    uint8_t secret[32];
    uint8_t signatureKey[32];
    float tempTarget;
    float rpmTarget;
    float phTarget;
} preferences_t;

void set_preferences(preferences_t *prefs);
preferences_t *get_preferences();
bool is_registered();

#endif