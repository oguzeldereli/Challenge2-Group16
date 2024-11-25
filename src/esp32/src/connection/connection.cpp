#include <WiFi.h>
#include <esp_wifi.h>
#include <WebSocketsClient.h>
#include "esp_eap_client.h"
#include "./connection.h"

bool wifi_wait()
{
    int max_attempts = 30;
    while (WiFi.status() != WL_CONNECTED && max_attempts > 0) {
        delay(500);
        max_attempts--;
    }

    if(WiFi.status() == WL_CONNECTED)
    {
        return true;
    }

    return false;
}

bool wifi_begin_enterprise(char *ssid, char *user, char *pass)
{
    WiFi.mode(WIFI_STA);
    esp_eap_client_set_identity((uint8_t *)user, strlen(user));
    esp_eap_client_set_username((uint8_t *)user, strlen(user));
    esp_eap_client_set_password((uint8_t *)pass, strlen(pass));
    esp_wifi_sta_enterprise_enable();
    WiFi.begin(ssid);

    return wifi_wait();
}

bool wifi_begin(char *ssid, char *pass)
{
    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid, pass);

    return wifi_wait();
}

WebSocketsClient webSocket;
void onWebSocketEvent(WStype_t type, uint8_t *payload, size_t length)
{
    switch (type)
    {
    case WStype_DISCONNECTED:
        Serial.println("Disconnected from WebSocket server");
        break;

    case WStype_CONNECTED:
        Serial.println("Connected to WebSocket server");
        webSocket.sendTXT("Hello from ESP32");
        break;

    case WStype_TEXT:
        Serial.print("Received message: ");
        Serial.println((char *)payload);
        break;

    case WStype_BIN:
        Serial.println("Received binary data");
        break;

    case WStype_PING:
        Serial.println("Received ping");
        break;

    case WStype_PONG:
        Serial.println("Received pong");
        break;

    case WStype_ERROR:
        Serial.println("WebSocket error");
        break;
    }
}

void websocket_begin()
{
    webSocket.begin(WEBSOCKET_SERVER, 443, "/ws");
    webSocket.onEvent(onWebSocketEvent);
}

void websocket_write_bin(uint8_t *data, uint16_t length)
{
    webSocket.sendBIN(data, length);
}

void websocket_keepalive()
{
    webSocket.loop();
}

uint8_t baseMac[6];
uint8_t *get_mac_address()
{
    esp_err_t ret = esp_wifi_get_mac(WIFI_IF_STA, baseMac);
    if (ret == ESP_OK)
    {
        return baseMac;
    }
    else
    {
        return 0;
    }
}