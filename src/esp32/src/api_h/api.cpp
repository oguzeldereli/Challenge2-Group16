#include "./api.h"
#include "../packets/packets.h"
#include "../connection/connection.h"

void register_client()
{
    uint16_t length;
    uint8_t *packet = write_register_request_normalized(&length);

    if(websocket_isConnected())
    {
        websocket_write_bin(packet, length);
    }
}   