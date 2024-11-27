#include "./api.h"
#include "../packets/packets.h"
#include "../connection/connection.h"
#include <cstring>

void ack_client()
{
    uint16_t length;
    uint8_t *packet = write_ack_normalized(&length);

    if (websocket_isConnected())
    {
        websocket_write_bin(packet, length);
    }
}

void register_client()
{
    uint16_t length;
    uint8_t *packet = write_register_request_normalized(&length);

    if (websocket_isConnected())
    {
        websocket_write_bin(packet, length);
    }
}

void auth_client()
{
    uint16_t length;
    uint8_t *packet = write_auth_request_normalized(&length);

    if (websocket_isConnected())
    {
        websocket_write_bin(packet, length);
    }
}

void revoke_auth_client()
{
    uint16_t length;
    uint8_t *packet = write_revoke_auth_request_normalized(&length);
    memset(auth_token, 0, 16);
    if (websocket_isConnected())
    {
        websocket_write_bin(packet, length);
    }
}

void send_data_to_server(uint8_t *data, uint32_t dataLength)
{
    uint16_t length;
    uint8_t *packet = write_data_packet_normalized(data, dataLength, &length);

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