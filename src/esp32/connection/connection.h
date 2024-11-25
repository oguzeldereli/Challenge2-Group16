#ifndef __CONNECTION_H__
#define __CONNECTION_H__

#include <cstdint>

void wifi_begin_enterprise(char *ssid, char *user, char *pass);
void wifi_begin(char *ssid, char *pass);
void websocket_begin();
void websocket_write_bin(char *data, uint16_t length);
void websocket_keepalive();

#define MAC_ADDRESS_SIZE 6

uint8_t *get_mac_address();

#endif