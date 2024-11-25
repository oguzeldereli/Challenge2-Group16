#include <WiFi.h>
#include "esp_eap_client.h"
#include <WebSocketsClient.h>
#include <ArduinoJson.h>

// Replace with your eduroam credentials
const char* ssid = "eduroam";
const char* USER = "xxxxxxxxx";
const char* PASS = "xxxxxxxxx";

// WebSocket server details
const char* websocket_host = "challenge2-group16-gui-webapi20241024170224.azurewebsites.net"; // Azure Web App Hostname
const uint16_t websocket_port = 443; // HTTPS port for WSS
const char* websocket_path = "/ws";
  
// Initialize WebSocketsClient
WebSocketsClient webSocket;

// Callback when data is received
void webSocketEvent(WStype_t type, uint8_t* payload, size_t length) {
    switch(type) {
        case WStype_DISCONNECTED:
            Serial.println("[WebSocket] Disconnected");
            break;
        case WStype_CONNECTED:
            Serial.println("[WebSocket] Connected");
            // Send a message to the server upon connection
            webSocket.sendTXT("Hello from ESP32");
            break;
        case WStype_TEXT:
            Serial.printf("[WebSocket] Received: %s\n", payload);
            break;
        case WStype_BIN:
            Serial.println("[WebSocket] Received binary data");
            break;
        case WStype_ERROR:
            Serial.println("[WebSocket] Error");
            break;
        case WStype_FRAGMENT_TEXT_START:
        case WStype_FRAGMENT_BIN_START:
        case WStype_FRAGMENT:
        case WStype_FRAGMENT_FIN:
            // Handle fragmented messages if necessary
            break;
        default:
            break;
    }
}

void setup() {
    Serial.begin(115200);
    delay(1000);

    // Connect to Wi-Fi with WPA2-Enterprise
    Serial.printf("Connecting to %s ", ssid);
    WiFi.mode(WIFI_STA);
    esp_eap_client_set_identity((uint8_t *)USER, strlen(USER));
    esp_eap_client_set_username((uint8_t *)USER, strlen(USER));
    esp_eap_client_set_password((uint8_t *)PASS, strlen(PASS));
    esp_wifi_sta_enterprise_enable();
    WiFi.begin(ssid);
    
    // Wait for connection
    Serial.println();
    int max_attempts = 30;
    while (WiFi.status() != WL_CONNECTED && max_attempts > 0) {
        delay(1000);
        Serial.print(".");
        max_attempts--;
    }
    
    if (WiFi.status() == WL_CONNECTED) {
        Serial.println("\nConnected to eduroam");
        Serial.print("IP Address: ");
        Serial.println(WiFi.localIP());

        // Initialize WebSocket with SSL
        webSocket.beginSSL(websocket_host, websocket_port, websocket_path, NULL, NULL);

        // Set WebSocket event handler
        webSocket.onEvent(webSocketEvent);

        // Optional: Set reconnect interval (default is 5000 ms)
        webSocket.setReconnectInterval(5000);
    } else {
        Serial.println("\nFailed to connect to eduroam");
    }
}

void loop() {
    webSocket.loop();
    
    // Example: Send a message every 10 seconds
    static unsigned long lastSendTime = 0;
    unsigned long currentMillis = millis();
    if (currentMillis - lastSendTime > 10000) { // 10 seconds

        lastSendTime = currentMillis;
        String message = "Ping from ESP32 at " + String(currentMillis / 1000) + " seconds";
        webSocket.sendTXT(message);
        Serial.println("Sent: " + message);
    }
}
