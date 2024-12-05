#include "./storage.h"
#include <Preferences.h>

const char *default_namespace = "cg16-ps";

void store_bytes(const char *key, void *bytes, size_t length)
{
    Preferences preferences;
    preferences.begin(default_namespace, false);
    preferences.putBytes(key, bytes, length);
    preferences.end();
}

uint8_t read_buffer[64];
uint8_t *get_read_buffer()
{
    return read_buffer;
}

uint8_t *read_bytes(const char *key, int length)
{
    Preferences preferences;
    preferences.begin(default_namespace, false);
    preferences.getBytes(key, read_buffer, length);
    preferences.end();

    return read_buffer;
}

void read_bytes_to(const char *key, uint8_t *position, size_t length)
{
    Preferences preferences;
    preferences.begin(default_namespace, false);
    preferences.getBytes(key, position, length);
    preferences.end();
}

void remove_key(const char *key)
{
    Preferences preferences;
    preferences.begin(default_namespace, false);
    preferences.remove(key);
    preferences.end();
}

void clear_storage()
{
    Preferences preferences;
    preferences.begin(default_namespace, false);
    preferences.clear();
    preferences.end();
}

void set_preferences(preferences_t *prefs)
{
    store_bytes("identifier", prefs->identifier, 32);
    store_bytes("secret", prefs->secret, 32);
    store_bytes("signature", prefs->signatureKey, 32);
    store_bytes("tempTarget", &prefs->tempTarget, 4);
    store_bytes("phTarget", &prefs->phTarget, 4);
    store_bytes("rpmTarget", &prefs->rpmTarget, 4);
}

preferences_t last_preferences;
preferences_t *get_preferences()
{
    read_bytes_to("identifier", last_preferences.identifier, 32);
    read_bytes_to("secret", last_preferences.secret, 32);
    read_bytes_to("signature", last_preferences.signatureKey, 32);
    read_bytes_to("tempTarget", (uint8_t *)&last_preferences.tempTarget, 4);
    read_bytes_to("phTarget", (uint8_t *)&last_preferences.phTarget, 4);
    read_bytes_to("rpmTarget", (uint8_t *)&last_preferences.rpmTarget, 4);

    return &last_preferences;
}

static uint8_t empty32[32];
bool is_registered()
{
    preferences_t * prefs = get_preferences();
    memset(empty32, 0, 32);
    return (memcmp(prefs->secret, empty32, 32) == 0 ? false : true) && (memcmp(prefs->signatureKey, empty32, 32) == 0 ? false : true);
}