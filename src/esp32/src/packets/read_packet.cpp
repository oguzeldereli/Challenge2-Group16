#include "./packets.h"
#include "../cryptography/crypt.h"
#include "../storage/storage.h"
#include "../api_h/api.h"
#include "../i2c/i2c.h"
#include <Arduino.h>
#include <cstring>
#include "time.h"

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
    memcpy(read_packet_buffer, read_normalized_packet_buffer, 60);                                                  // copy fixed parts
    memcpy(read_packet_buffer + sizeof(data_packet_model_t), read_normalized_packet_buffer + 60, packet->dataSize); // copy data
    packet->data = (uint8_t *)(read_packet_buffer + sizeof(data_packet_model_t));
    memcpy(packet->packetSignature, read_normalized_packet_buffer + 60 + packet->dataSize, 32); // copy signature
    return packet;
}

void handle_data(uint8_t *data, uint32_t dataSize, uint8_t *packetIdentifier)
{
    if (dataSize == 0)
    {
        return;
    }

    uint8_t flag = data[0];
    uint8_t packetType = (flag & 0b00000001);
    uint8_t dataType = (flag & 0b00000110) >> 1;
    uint8_t command = (flag & 0b00111000) >> 3;

    if (dataType != 0 || dataType != 0)
    {
        return;
    }

    if (command == 2) // command
    {
        if (dataSize < 2)
        {
            return;
        }

        uint8_t exec_command = data[1];
        if (exec_command == 0xff) // start
        {
            set_status(1);
            ack_server(packetIdentifier);
        }
        else if (exec_command == 0x00) // pause
        {
            set_status(2);
            ack_server(packetIdentifier);
        }
        else if (exec_command == 0x01) // set target
        {
            preferences_t *prefs = get_preferences();
            uint8_t dataType = data[2]; // 0 for temp, 1 for ph, 2 for rpm
            float value = *((double *)data + 3);
            if (dataType == 0)
            {
                prefs->tempTarget = value;
            }
            else if (dataType == 1)
            {
                prefs->phTarget = value;
            }
            else if (dataType == 2)
            {
                prefs->rpmTarget = value;
            }
            i2c_write(data + 2, 5); // send full packet data to arduino
            set_preferences(prefs);
            ack_server(packetIdentifier);
        }
        else if (exec_command == 0x02) // get status
        {
            send_status_to_server(getTime());
        }
    }

    return; // we dont support anything but commands
}

void handle_packet(data_packet_model_t *packet)
{
    switch (packet->packetType)
    {
    case 0: // ack
        // ignore the acks for now
        break;
    case 1: // register
    {
        // store secret and signature key
        preferences_t *prefs = get_preferences();
        memcpy(prefs->secret, packet->data, 32);
        memcpy(prefs->signatureKey, packet->data + 32, 32);
        set_preferences(prefs);
        break;
    }
    case 2: // auth
    {
        // store auth token
        store_auth_token(packet->data);
        ack_server(packet->packetIdentifier);
        break;
    }
    case 3: // revoke auth
    {
        // server will never send a revoke auth packet
        // just ignore it
        ack_server(packet->packetIdentifier);
        break;
    }
    case 4: // data
    {
        handle_data(packet->data, packet->dataSize, packet->packetIdentifier);
        break;
    }
    case 5: // error
    {
        if (Serial)
        {
            Serial.print("Received Error: ");
            if (packet->packetError == 1)
            {
                Serial.println("Malformed Packet");
            }
            else if (packet->packetError == 2)
            {
                Serial.println("Invalid Packet");
            }
            else if (packet->packetError == 3)
            {
                Serial.println("Internal Error");
            }
        }
        ack_server(packet->packetIdentifier);
        break;
    }
    default:
        break;
    }
}