#include <WiFi.h>
#include <WebSocketsClient.h>
#include "esp_eap_client.h"
#include "./connection.h"

void wifi_begin_enterprise(char *ssid, char *user, char *pass)
{
    WiFi.mode(WIFI_STA);
    esp_eap_client_set_identity((uint8_t *)user, strlen(user));
    esp_eap_client_set_username((uint8_t *)user, strlen(user));
    esp_eap_client_set_password((uint8_t *)pass, strlen(pass));
    esp_wifi_sta_enterprise_enable();
    WiFi.begin(ssid);
}

void wifi_begin(char *ssid, char *pass)
{
    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid, pass);
}

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

WebSocketsClient webSocket;
void websocket_begin()
{
    webSocket.begin(websocket_server);
    webSocket.onEvent(onWebSocketEvent);
}

void websocket_write_bin(char *data, uint16_t length)
{
    webSocket.sendBIN(data, length);
}

void websocket_keepalive()
{
    webSocket.loop();
}