#ifndef __PACKETS_H__
#define __PACKETS_H__

#include <cstdint>

#define MAXIMUM_PACKET_SIZE 4096

typedef struct
{
    char signature[8];
    uint64_t sentAt;
    char packetIdentifier[16];
    char authorizationToken[16];
    uint32_t packetType;
    uint32_t packetError;
    uint32_t packetEncryptionMethod;
    uint32_t dataSize;
    char *encryptedData;
    char packetSignature[32];
} data_packet_model_t;

typedef struct
{
    char packetFlag;
    char *data;
} packet_data_format_t;

typedef struct
{
    char dataType;
    uint64_t dataCount;
    char *data;
} data_write_packet_format_t;

typedef struct
{
    char command;
    char *data;
} command_packet_format_t;

#endif