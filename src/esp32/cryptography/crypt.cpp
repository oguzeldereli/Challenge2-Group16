#include <SHA256.h>
#include <HMAC.h>
#include <crypt.h>
#include <AES.h>
#include "esp_random.h"

uint8_t last_signature[32];
uint8_t *sign_packet(uint8_t *key, uint8_t *bytes, size_t length)
{
    HMAC<SHA256> hmac;
    hmac.begin(key, 32);
    hmac.update(bytes, length);
    hmac.finalize(last_signature, sizeof(last_signature));

    return last_signature;
}

bool validate_packet(uint8_t *key, uint8_t *bytes, size_t length, uint8_t *signature)
{
    HMAC<SHA256> hmac;

    hmac.begin(key, 32);
    hmac.update(bytes, length);
    uint8_t computedHMAC[32];
    hmac.finalize(computedHMAC, sizeof(computedHMAC));

    bool isValid = true;
    for (size_t i = 0; i < sizeof(computedHMAC); i++)
    {
        if (computedHMAC[i] != signature[i])
        {
            isValid = false;
            break;
        }
    }

    return isValid;
}

uint8_t last_hash[32];
uint8_t *sha256_hash(const char *message, size_t length)
{
    SHA256 sha256;
    sha256.update(input, strlen(input));
    sha256.finalize(last_hash, sizeof(last_hash));

    return last_hash;
}

void fill_random_array(void *buf, size_t len)
{
    esp_fill_random(buf, len);
}