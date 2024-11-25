#ifndef __CONNECTION_H__
#define __CONNECTION_H__

#include <cstdint>

bool wifi_begin_enterprise(char *ssid, char *user, char *pass);
bool wifi_begin(char *ssid, char *pass);
void websocket_begin();
void websocket_write_bin(uint8_t *data, uint16_t length);
void websocket_keepalive();

#define MAC_ADDRESS_SIZE 6
#define WEBSOCKET_SERVER "wss://some-website.com"

uint8_t *get_mac_address();

#endif