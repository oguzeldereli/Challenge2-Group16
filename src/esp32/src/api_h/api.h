#ifndef __API_H__
#define __API_H__

#include <cstdint>

uint64_t getTime();
void ack_server(uint8_t *chainIdentifier);
void register_client();
void auth_client();
void revoke_auth_client();
void revoke_auth();
void send_data_to_server(uint8_t *data, uint32_t dataLength);
void send_value_to_server(uint8_t datatType, uint64_t timeStamp, float value, uint8_t *chainIdentifier);
void send_status_to_server(uint64_t timeStamp, uint8_t *chainIdentifier);
void send_log_to_server(uint64_t timeStamp, char *level, uint32_t levelLength, char *message, uint32_t messageLength);

void store_auth_token(uint8_t *token);
uint8_t *get_auth_token();
bool is_auth_token_empty();

uint32_t get_status();
void set_status(uint32_t s);

#endif