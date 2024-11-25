#include "./packets.h"
#include "../cryptography/crypt.h"
#include "../storage/storage.h"
#include <cstring>
#include "time.h"

char write_packet_buffer[MAXIMUM_PACKET_SIZE + 128];
char write_normalized_packet_buffer[MAXIMUM_PACKET_SIZE];

char valid_packet_signature[8] = {1, 16, 'e', 's', 'p', 'c', 'o', 'm'};
void create_packet_on_buffer()
{
    memset(write_packet_buffer, 0, MAXIMUM_PACKET_SIZE);
    data_packet_model_t *packet = write_packet_buffer;
    memcpy(packet->signature, valid_packet_signature, 8);
    memcpy(packet->sentAt, getTime(), 8);
    memcpy(packet->sentAt, getTime(), 8);
    fill_random_array(packet->packetIdentifier, 16);
    memset(packet->authorizationToken, 0, 16);
    packet->packetType = 0;
    packet->packetError = 0;
    packet->dataSize = 0;
    packet->data = packet_get_buffer + sizeof(data_packet_model_t);
    memset(packet->packetSignature, 0, 32);
}

void set_packet_type_on_buffer(uint32_t type)
{
    data_packet_model_t *packet = write_packet_buffer;
    packet->packetType = type;
}

void set_packet_error_on_buffer(uint32_t error)
{
    data_packet_model_t *packet = write_packet_buffer;
    packet->packetError = error;
}

void set_packet_data_on_buffer(uint8_t *data, uint32_t size)
{
    data_packet_model_t *packet = write_packet_buffer;
    set_packet_data_size_on_buffer(size);
    if (packet->data == 0)
    {
        packet->data = write_packet_buffer + sizeof(data_packet_model_t);
    }
    memcpy(packet->data, data, size);
    packet->dataSize = size;
    packet->data = data;
}

void set_packet_auth_token_on_buffer(uint8_t *authToken)
{
    data_packet_model_t *packet = write_packet_buffer;
    memcpy(packet->authorizationToken, authToken, 16);
}

void set_packet_identifier_on_buffer(uint8_t *identifier)
{
    data_packet_model_t *packet = write_packet_buffer;
    memcpy(packet->packetIdentifier, authToken, 16);
}

void and_sign_packet_on_buffer()
{
    memset(write_normalized_packet_buffer, 0, MAXIMUM_PACKET_SIZE);
    data_packet_model_t *packet = write_packet_buffer;
    memcpy(write_normalized_packet_buffer, packet, 60);
    uint length = 60;
    if (packet->data != 0 && packet->dataSize > 0)
    {
        length += dataSize;
        memcpy(write_normalized_packet_buffer + 60, packet->data, dataSize);
    }
    preferences_t *prefs = get_preferences();
    uint8_t *hash = sign_packet(prefs->signatureKey, write_normalized_packet_buffer, length);
    memcpy(packet->packetSignature, hash, 32);
}

uint8_t *write_normalized_packet(uint *dataLength)
{
    memset(write_normalized_packet_buffer, 0, MAXIMUM_PACKET_SIZE);
    data_packet_model_t *packet = write_packet_buffer;
    memcpy(write_normalized_packet_buffer, packet, 60); // copy packet metadata
    uint length = 60;
    if (packet->data != 0 && packet->dataSize > 0)
    {
        length += dataSize;
        memcpy(write_normalized_packet_buffer + 60, packet->data, dataSize); // copy packet data
    }
    memcpy(write_normalized_packet_buffer + 60 + dataSize, packet->packetSignature, 32); // copy packet signature
    dataLength = 60 + dataSize;
    return write_normalized_packet_buffer;
}
