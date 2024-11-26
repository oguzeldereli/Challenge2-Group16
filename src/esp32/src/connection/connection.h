#ifndef __CONNECTION_H__
#define __CONNECTION_H__

#include <cstdint>

bool wifi_begin_enterprise(char *ssid, char *user, char *pass);
bool wifi_begin(char *ssid, char *pass);
void websocket_begin();
void websocket_write_bin(uint8_t *data, uint16_t length);
void websocket_keepalive();
bool websocket_isConnected();

#define MAC_ADDRESS_SIZE 6
#define WEBSOCKET_SERVER "tjvdds4c-443.uks1.devtunnels.ms"

uint8_t *get_mac_address();

#endif