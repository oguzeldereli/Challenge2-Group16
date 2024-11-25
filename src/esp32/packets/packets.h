#ifndef __PACKETS_H__
#define __PACKETS_H__

#include <cstdint>

#define MAXIMUM_PACKET_SIZE 1024

typedef struct
{
    char signature[8];
    uint64_t sentAt;
    char packetIdentifier[16];
    char authorizationToken[16];
    uint32_t packetType;
    uint32_t packetError;
    uint32_t dataSize;
    char *data;
    char packetSignature[32];
} data_packet_model_t;

#endif