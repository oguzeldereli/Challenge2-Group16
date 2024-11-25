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

#endif