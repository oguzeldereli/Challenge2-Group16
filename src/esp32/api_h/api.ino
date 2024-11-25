#include "./api.h"
#include "../connection/connection.h"
#include "../cryptography/crypt.h"
#include "../storage/storage.h"
#include <cstring>

void generate_store_identifier()
{
    uint8_t *mac_address = get_mac_address();
    uint8_t *hash = sha256_hash(mac_address, MAC_ADDRESS_SIZE);
    preferences_t *prefs = get_preferences();
    memcpy(&prefs->identifier, hash, 32);
    set_preferences(prefs);
}
