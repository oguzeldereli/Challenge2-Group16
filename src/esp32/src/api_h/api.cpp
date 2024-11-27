#include "./api.h"
#include "../packets/packets.h"
#include "../connection/connection.h"

void register_client()
{
    uint16_t length;
    uint8_t *packet = write_register_request_normalized(&length);

    if (websocket_isConnected())
    {
        websocket_write_bin(packet, length);
    }
}

uint8_t auth_token[16];
void store_auth_token(uint8_t *token)
{
    memcpy(auth_token, token, 16);
}

uint8_t *get_auth_token()
{
    return auth_token;
}

uint8_t auth_token_check[16];
bool is_auth_token_empty()
{
    memset(auth_token_check, 0, 16);
    return memcmp(auth_token, auth_token_check, 16) == 0 ? true : false;
}