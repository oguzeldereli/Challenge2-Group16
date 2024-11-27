#ifndef __API_H__
#define __API_H__

void register_client();
void store_auth_token(uint8_t *token);
uint8_t *get_auth_token();
bool is_auth_token_empty()

#endif