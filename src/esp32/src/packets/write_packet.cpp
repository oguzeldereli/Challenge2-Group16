#include "./packets.h"
#include "../cryptography/crypt.h"
#include "../storage/storage.h"
#include "../connection/connection.h"
#include "../api_h/api.h"
#include <cstring>
#include <Arduino.h>
#include "time.h"

uint8_t write_packet_buffer[MAXIMUM_PACKET_SIZE + 8]; // +8 because there is a single pointer allocation extra
uint8_t write_normalized_packet_buffer[MAXIMUM_PACKET_SIZE];

uint8_t valid_packet_signature[8] = {1, 16, 'e', 's', 'p', 'c', 'o', 'm'};
data_packet_model_t *create_packet_on_buffer()
{
    memset(write_packet_buffer, 0, MAXIMUM_PACKET_SIZE);
    data_packet_model_t *packet = (data_packet_model_t *)write_packet_buffer;
    memcpy(packet->signature, valid_packet_signature, 8);
    packet->sentAt = getTime();
    fill_random_array(packet->packetIdentifier, 16);
    memset(packet->authorizationToken, 0, 16);
    packet->packetType = 0;
    packet->packetError = 0;
    packet->dataSize = 0;
    packet->data = write_packet_buffer + sizeof(data_packet_model_t);
    memset(packet->packetSignature, 0, 32);

    return packet;
}

void set_packet_type_on_buffer(uint32_t type)
{
    data_packet_model_t *packet = (data_packet_model_t *)write_packet_buffer;
    packet->packetType = type;
}

void set_packet_error_on_buffer(uint32_t error)
{
    data_packet_model_t *packet = (data_packet_model_t *)write_packet_buffer;
    packet->packetError = error;
}

void set_packet_data_on_buffer(uint8_t *data, uint32_t size)
{
    data_packet_model_t *packet = (data_packet_model_t *)write_packet_buffer;

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
    data_packet_model_t *packet = (data_packet_model_t *)write_packet_buffer;
    memcpy(packet->authorizationToken, authToken, 16);
}

void set_packet_identifier_on_buffer(uint8_t *identifier)
{
    data_packet_model_t *packet = (data_packet_model_t *)write_packet_buffer;
    memcpy(packet->packetIdentifier, identifier, 16);
}

void sign_packet_on_buffer()
{
    memset(write_normalized_packet_buffer, 0, MAXIMUM_PACKET_SIZE);
    data_packet_model_t *packet = (data_packet_model_t *)write_packet_buffer;
    memcpy(write_normalized_packet_buffer, packet, 60);
    uint length = 60;
    if (packet->data != 0 && packet->dataSize > 0)
    {
        length += packet->dataSize;
        memcpy(write_normalized_packet_buffer + 60, packet->data, packet->dataSize);
    }
    preferences_t *prefs = get_preferences();
    uint8_t *hash = sign_packet(prefs->signatureKey, write_normalized_packet_buffer, length);
    memcpy(packet->packetSignature, hash, 32);
}

uint8_t *write_normalized_packet(uint16_t *dataLength)
{
    memset(write_normalized_packet_buffer, 0, MAXIMUM_PACKET_SIZE);
    data_packet_model_t *packet = (data_packet_model_t *)write_packet_buffer;
    memcpy(write_normalized_packet_buffer, packet, 60); // copy packet metadata
    uint length = 60;
    if (packet->data != 0 && packet->dataSize > 0)
    {
        length += packet->dataSize;
        memcpy(write_normalized_packet_buffer + 60, packet->data, packet->dataSize); // copy packet data
    }
    memcpy(write_normalized_packet_buffer + 60 + packet->dataSize, packet->packetSignature, 32); // copy packet signature
    *dataLength = 60 + packet->dataSize + 32;
    return write_normalized_packet_buffer;
}

void generate_store_identifier()
{
    uint8_t *mac_address = get_mac_address();
    uint8_t *hash = sha256_hash(mac_address, MAC_ADDRESS_SIZE);
    preferences_t *prefs = get_preferences();
    memcpy(prefs->identifier, hash, 32);
    set_preferences(prefs);
}

uint8_t *write_ack_normalized(uint8_t *chainIdentifier, uint16_t *packetLength)
{
    data_packet_model_t *packet = create_packet_on_buffer();
    packet->packetType = 0; // ack packet
    uint8_t *auth_token = get_auth_token();
    memcpy(packet->authorizationToken, auth_token, 16);
    packet->packetError = 0;
    packet->dataSize = 0;
    set_packet_identifier_on_buffer(chainIdentifier);
    sign_packet_on_buffer();
    return write_normalized_packet(packetLength);
}

uint8_t *write_register_request_normalized(uint16_t *packetLength)
{
    data_packet_model_t *packet = create_packet_on_buffer();
    packet->packetType = 1; // register packet
    memset(packet->authorizationToken, 0, 16);
    packet->packetError = 0;
    uint8_t data[36];
    generate_store_identifier();
    preferences_t *prefs = get_preferences();
    memset(data, 0, 36);
    memcpy(data, prefs->identifier, 32); // client identifier (hash of mac address)
    data[32] = 0x01;                     // confidential client
    set_packet_data_on_buffer(data, 36);
    memset(packet->packetSignature, 0, 32);
    return write_normalized_packet(packetLength);
}

uint8_t *write_auth_request_normalized(uint16_t *packetLength)
{
    data_packet_model_t *packet = create_packet_on_buffer();
    packet->packetType = 2; // auth packet
    memset(packet->authorizationToken, 0, 16);
    packet->packetError = 0;
    uint8_t data[64];
    preferences_t *prefs = get_preferences();
    memset(data, 0, 64);
    memcpy(data, prefs->identifier, 32);  // client identifier
    memcpy(data + 32, prefs->secret, 32); // client secret
    set_packet_data_on_buffer(data, 64);
    memset(packet->packetSignature, 0, 32);
    return write_normalized_packet(packetLength);
}

uint8_t *write_revoke_auth_request_normalized(uint16_t *packetLength)
{
    data_packet_model_t *packet = create_packet_on_buffer();
    packet->packetType = 3; // revoke auth packet
    uint8_t *auth_token = get_auth_token();
    memcpy(packet->authorizationToken, auth_token, 16);
    packet->packetError = 0;
    packet->dataSize = 0;
    sign_packet_on_buffer();
    return write_normalized_packet(packetLength);
}

uint8_t *write_data_packet_normalized(uint8_t *data, uint32_t length, uint16_t *packetLength)
{
    data_packet_model_t *packet = create_packet_on_buffer();
    packet->packetType = 4; // data packet
    uint8_t *auth_token = get_auth_token();
    memcpy(packet->authorizationToken, auth_token, 16);
    packet->packetError = 0;
    packet->dataSize = length;
    memcpy(packet->data, data, length);
    sign_packet_on_buffer();
    return write_normalized_packet(packetLength);
}

uint8_t *write_data_value_packet_normalized(uint8_t dataType, uint64_t timeStamp, float data, uint16_t *packetLength, uint8_t *chainIdentifier)
{
    // 1 byte packet flag = 0b00001000 binary data store
    // 1 byte data type = dataType
    // 8 bytes data count = 1
    // 8 bytes timestamp = timeStamp
    // 8 bytes value = data
    // total 26 bytes

    data_packet_model_t *packet = create_packet_on_buffer();
    if(chainIdentifier != 0)
    {
        memcpy(packet->packetIdentifier, chainIdentifier, 16);
    }
    packet->packetType = 4; // data packet
    uint8_t *auth_token = get_auth_token();
    memcpy(packet->authorizationToken, auth_token, 16);
    packet->packetError = 0;
    packet->dataSize = 26;

    uint8_t packetFlag = 0b00001000;
    memcpy(packet->data, &packetFlag, sizeof(uint8_t));
    memcpy(packet->data + 1, &dataType, sizeof(uint8_t));

    uint64_t dataCount = 1;
    memcpy(packet->data + 2, &dataCount, sizeof(uint64_t));
    memcpy(packet->data + 10, &timeStamp, sizeof(uint64_t));
    memcpy(packet->data + 18, &data, sizeof(float));

    sign_packet_on_buffer();
    return write_normalized_packet(packetLength);
}

uint8_t *write_device_status_packet_normalized(uint64_t timeStamp, uint32_t status, uint16_t *packetLength, uint8_t *chainIdentifier)
{
    // 1 byte packet flag = 0b00001000 binary data store
    // 1 byte data type = 3 device status data
    // 8 bytes data count = 1 a single status
    // 8 bytes timestamp = timeStamp
    // 4 bytes value = status
    // 8 bytes value = tempTarget
    // 8 bytes value = phTarget
    // 8 bytes value = rpmTarget
    // total 46 bytes

    data_packet_model_t *packet = create_packet_on_buffer();
    if(chainIdentifier != 0)
    {
        memcpy(packet->packetIdentifier, chainIdentifier, 16);
    }
    packet->packetType = 4; // data packet
    uint8_t *auth_token = get_auth_token();
    memcpy(packet->authorizationToken, auth_token, 16);
    packet->packetError = 0;
    packet->dataSize = 34;
    
    uint8_t packetFlag = 0b00001000;
    memcpy(packet->data, &packetFlag, sizeof(uint8_t));
    uint8_t dataType = 3;
    memcpy(packet->data + 1, &dataType, sizeof(uint8_t));

    preferences_t *prefs = get_preferences();

    uint64_t dataCount = 1;
    memcpy(packet->data + 2, &dataCount, sizeof(uint64_t));
    memcpy(packet->data + 10, &timeStamp, sizeof(uint64_t));
    memcpy(packet->data + 18, &status, sizeof(uint32_t));
    memcpy(packet->data + 22, &prefs->tempTarget, sizeof(float));
    memcpy(packet->data + 26, &prefs->phTarget, sizeof(float));
    memcpy(packet->data + 30, &prefs->rpmTarget, sizeof(float));

    sign_packet_on_buffer();
    return write_normalized_packet(packetLength);
}

uint8_t *write_log_packet_normalized(uint64_t timeStamp, char *level, uint32_t levelLength, char *message, uint32_t messageLength, uint16_t *packetLength)
{
    // 1 byte packet flag = 0b00001000 binary data store
    // 1 byte data type = 4 log data
    // 8 bytes data count = 1 a single log
    // 8 bytes timestamp = timeStamp
    // 4 bytes value = levelLength
    // levelLength bytes value = level
    // 4 bytes value = messageLength
    // messageLength bytes value = message
    // total 26 + messageLength + levelLength bytes

    data_packet_model_t *packet = create_packet_on_buffer();
    packet->packetType = 4; // data packet
    uint8_t *auth_token = get_auth_token();
    memcpy(packet->authorizationToken, auth_token, 16);
    packet->packetError = 0;
    packet->dataSize = 26 + levelLength + messageLength;
    
    uint8_t packetFlag = 0b00001000;
    memcpy(packet->data, &packetFlag, sizeof(uint8_t));
    uint8_t dataType = 4;
    memcpy(packet->data + 1, &dataType, sizeof(uint8_t));

    uint64_t dataCount = 1;
    memcpy(packet->data + 2, &dataCount, sizeof(uint64_t));
    memcpy(packet->data + 10, &timeStamp, sizeof(uint64_t));
    memcpy(packet->data + 18, &levelLength, sizeof(uint32_t));
    memcpy(packet->data + 22, level, levelLength);
    memcpy(packet->data + 22 + levelLength, &messageLength, sizeof(uint32_t));
    memcpy(packet->data + 26 + levelLength, message, messageLength);

    sign_packet_on_buffer();
    return write_normalized_packet(packetLength);
}