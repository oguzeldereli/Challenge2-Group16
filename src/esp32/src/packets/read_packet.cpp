#include "./packets.h"
#include "../cryptography/crypt.h"
#include "../storage/storage.h"
#include "../api_h/api.h"

uint8_t read_packet_buffer[MAXIMUM_PACKET_SIZE];
uint8_t read_normalized_packet_buffer[MAXIMUM_PACKET_SIZE];

uint8_t *get_packet_read_buffer()
{
    return read_normalized_packet_buffer;
}

bool validate_packet_signature_on_buffer()
{
    data_packet_model_t *halfPacket = (data_packet_model_t *)read_normalized_packet_buffer;
    preferences_t *prefs = get_preferences();
    uint8_t *signatureKey = read_normalized_packet_buffer + 60 + halfPacket->dataSize;
    return validate_packet(prefs->signatureKey, read_normalized_packet_buffer, 60 + halfPacket->dataSize, signatureKey);
}

void read_normalized_packet(uint8_t *data, uint16_t length)
{
    memset(read_normalized_packet_buffer, 0, MAXIMUM_PACKET_SIZE);
    memcpy(read_normalized_packet_buffer, data, length);
}

data_packet_model_t *structurize_packet()
{
    data_packet_model_t *packet = (data_packet_model_t *)read_packet_buffer;
    memcpy(packet, read_normalized_packet_buffer, 60);                                                  // copy fixed parts
    memcpy(packet + sizeof(data_packet_model_t), read_normalized_packet_buffer + 60, packet->dataSize); // copy data
    memcpy(packet->packetSignature, read_normalized_packet_buffer + 60 + packet->dataSize, 32);         // copy signature
    return packet;
}

void handle_data(uint8_t *data, uin32_t dataSize)
{
}

void handle_packet(data_packet_model_t *packet)
{
    switch (packet->packetType)
    {
    case 0: // ack
        // ignore the acks for now
        break;
    case 1: // register
        // store secret and signature key
        preferences_t *prefs = get_preferences();
        memcpy(prefs->secret, packet->data, 32);
        memcpy(prefs->signatureKey, packet->data + 32, 32);
        set_preferences(prefs);
        break;
    case 2: // auth
        // store auth token
        store_auth_token(packet->data);
        break;
    case 3: // revoke auth
        // server will never send a revoke auth packet
        // just ignore it
        break;
    case 4: // data
        handle_data(packet->data, packet->dataSize);
        break;
    case 5: // error
        // ignore the errors for now
        break;
    default:
        break;
    }
}