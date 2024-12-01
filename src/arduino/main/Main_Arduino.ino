#include "PI_Motor_Control.h"
#include <Wire.h>

float desired_speed = 0;
float desired_temp = 0;
float desired_pH = 0;

void getDesiredParameters() {
  uint8_t current_parameter[4];
  uint8_t system_identifier;

  while (Wire.available()) {
    uint8_t current_byte = Wire.read();
    
    for (int i = 0; i < 5; i++) {
      if (i < 4) {
        current_parameter[i] = current_byte;  
      } else {
        system_identifier = current_byte;
      }
    }

    float parameter;
    memcpy(&parameter, current_parameter, sizeof(float));

    switch (system_identifier) {
      case 0:
        desired_speed = parameter;
        break;
      case 1:
        desired_temp = parameter;
        break;
      case 2:
        desired_pH = parameter;
        break;
    }
  }
}

void writeCurrentParameters(float parameter, uint8_t identifier) {
  Wire.beginTransmission(esp32_address);
  uint8_t *bytes = (uint8_t *)(&parameter);
  Wire.write(bytes, sizeof(parameter));
  Wire.write(identifier);
  Wire.endTransmission();
}

void setup() {
  setupStirring();

  Wire.begin();

  TCCR1A = 0b00000011; // 10 bit
  TCCR1B = 0b00000001; // 7.8 kHz

  Serial.begin(9600);
}

void loop() {
  Wire.onReceive(getDesiredParameters);

  motor_speed = runStirring(desired_speed);
  writeCurrentParameters(motor_speed, 0);
}
