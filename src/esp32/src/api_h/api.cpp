#include "./api.h"
#include "../packets/packets.h"
#include "../connection/connection.h"
#include <Arduino.h>
#include "time.h"
#include <cstring>

uint64_t getTime() 
{
  return (uint64_t)time(nullptr);
}

void ack_server(uint8_t *chainIdentifier)
{
    uint16_t length;
    uint8_t *packet = write_ack_normalized(chainIdentifier, &length);

    if (websocket_isConnected() && !is_auth_token_empty())
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
    memset(get_auth_token(), 0, 16);
    if (websocket_isConnected() && !is_auth_token_empty())
    {
        websocket_write_bin(packet, length);
    }
}

void send_data_to_server(uint8_t *data, uint32_t dataLength)
{
    uint16_t length;
    uint8_t *packet = write_data_packet_normalized(data, dataLength, &length);

    if (websocket_isConnected() && !is_auth_token_empty())
    {
        websocket_write_bin(packet, length);
    }
}

void send_value_to_server(uint8_t datatType, uint64_t timeStamp, double value)
{
    uint16_t length;
    uint8_t *packet = write_data_value_packet_normalized(datatType, timeStamp, value, &length);

    if (websocket_isConnected() && !is_auth_token_empty())
    {
        websocket_write_bin(packet, length);
    }
}

void send_status_to_server(uint64_t timeStamp)
{
    uint16_t length;
    uint8_t *packet = write_device_status_packet_normalized(timeStamp, get_status(), &length);

    if (websocket_isConnected() && !is_auth_token_empty())
    {
        websocket_write_bin(packet, length);
    }
}

void send_log_to_server(uint64_t timeStamp, char *level, uint32_t levelLength, char *message, uint32_t messageLength)
{
    uint16_t length;
    uint8_t *packet = write_log_packet_normalized(timeStamp, level, levelLength, message, messageLength, &length);

    if (websocket_isConnected() && !is_auth_token_empty())
    {
        websocket_write_bin(packet, length);
    }
}

static uint8_t auth_token[16];
void store_auth_token(uint8_t *token)
{
    memcpy(auth_token, token, 16);
}

uint8_t *get_auth_token()
{
    return auth_token;
}

static uint8_t auth_token_check[16];
bool is_auth_token_empty()
{
    memset(auth_token_check, 0, 16);
    return memcmp(auth_token, auth_token_check, 16) == 0 ? true : false;
}

static uint32_t status;
uint32_t get_status()
{
    return status;
}

void set_status(uint32_t s)
{
    status = s;
}