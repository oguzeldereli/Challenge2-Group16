#include "./packets.h"
#include "../cryptography/crypt.h"
#include "../storage/storage.h"

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
    memcpy(packet, read_normalized_packet_buffer, 60); // copy fixed parts
    memcpy(packet + sizeof(data_packet_model_t), read_normalized_packet_buffer + 60, packet->dataSize); // copy data
    memcpy(packet->packetSignature, read_normalized_packet_buffer + 60 + packet->dataSize, 32); // copy signature
    return packet;
}

void handle_packet(data_packet_model_t *packet)
{
    
}