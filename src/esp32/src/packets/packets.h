#ifndef __PACKETS_H__
#define __PACKETS_H__

#include <cstdint>

#define MAXIMUM_PACKET_SIZE 1024

typedef struct
{
    uint8_t signature[8];
    uint64_t sentAt;
    uint8_t packetIdentifier[16];
    uint8_t authorizationToken[16];
    uint32_t packetType;
    uint32_t packetError;
    uint32_t dataSize;
    uint8_t *data;
    uint8_t packetSignature[32];
} data_packet_model_t;

void read_normalized_packet(uint8_t *data, uint16_t length);
data_packet_model_t *structurize_packet();
void handle_packet(data_packet_model_t *packet);

uint8_t *write_ack_normalized(uint8_t *chainIdentifier, uint16_t *packetLength);
uint8_t *write_register_request_normalized(uint16_t *packetLength);
uint8_t *write_auth_request_normalized(uint16_t *packetLength);
uint8_t *write_revoke_auth_request_normalized(uint16_t *packetLength);
uint8_t *write_data_packet_normalized(uint8_t *data, uint32_t length, uint16_t *packetLength);
uint8_t *write_data_value_packet_normalized(uint8_t dataType, uint64_t timeStamp, double data, uint16_t *packetLength);
uint8_t *write_device_status_packet_normalized(uint64_t timeStamp, uint32_t status, uint16_t *packetLength);
uint8_t *write_log_packet_normalized(uint64_t timeStamp, char *level, uint32_t levelLength, char *message, uint32_t messageLength, uint16_t *packetLength);
#endif