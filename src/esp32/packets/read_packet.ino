#include "./packets.h"

char read_normalized_packet_buffer[MAXIMUM_PACKET_SIZE];

uint8_t *get_read_buffer()
{
    return read_normalized_packet_buffer;
}

bool validate_packet_signature_on_buffer()
{
    data_packet_model_t *halfPacket = read_normalized_packet_buffer;
    preferences_t *prefs = get_preferences();
    uint8_t *signatureKey = read_normalized_packet_buffer + 60 + halfPacket->dataSize;
    return validate_packet(prefs->signatureKey, read_normalized_packet_buffer, 60 + halfPacket->dataSize, signatureKey);
}