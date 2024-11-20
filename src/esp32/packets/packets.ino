#include "./packets.h"

// First bit signifies whether data is available in buffer for use
// Secnod bit signifies whether an error occured during reading from char array
// Third bit signifies whether an error occured during converting into char array
// Fourth bit is reserved
// The rest of the 12 bits are the length of data inside the packet_buffer
uint16_t packet_buffer_flag = 0x00;
uint16_t *packet_get_flag()
{
    return &packet_buffer_flag;
}

uint16_t packet_get_buffer_length()
{
    return packet_buffer_flag >> 4;
}

char packet_buffer[MAXIMUM_PACKET_SIZE];
char *packet_get_buffer(uint16_t *length)
{
    *length = packet_get_buffer_length();
    return packetBuffer;
}
