#ifndef __API_H__
#define __API_H__

void ack_client();
void register_client();
void revoke_auth_client();
void revoke_auth();
void send_data_to_server(uint8_t *data, uint32_t dataLength);

void store_auth_token(uint8_t *token);
uint8_t *get_auth_token();
bool is_auth_token_empty()

#endif