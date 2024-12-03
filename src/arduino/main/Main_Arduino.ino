#include "PI_Motor_Control.h"
#include "PI_Heating_Control.h"
#include "PI_pH_Control.h"
#include <Wire.h>
#include <EEPROM.h>

#define ESP32_I2C_ADDRESS 0x11
#define ARDUINO_I2C_ADRESS 0x12
#define WRITE_INTERVAL 100

#define EEPROM_ADDRESS_TEMP 0
#define EEPROM_ADDRESS_PH 8
#define EEPROM_ADDRESS_SPEED 16

// Desired Parameters
double desired_speed = 0;
double desired_temp = 298.15;
double desired_pH = 5.0;

// Current Parameters
double current_speed = 0;
double current_temp = 0;
double current_pH = 0;

// Timing Variable
unsigned long previous_write_time = 0;

void loadParametersEEPROM() {
  EEPROM.get(EEPROM_ADDRESS_TEMP, desired_temp);
  EEPROM.get(EEPROM_ADDRESS_PH, desired_pH);
  EEPROM.get(EEPROM_ADDRESS_SPEED, desired_speed);

  Serial.println("Parameters loaded from EEPROM :");
  Serial.print("Temperature: "); 
  Serial.println(desired_temp);
  Serial.print("pH: "); 
  Serial.println(desired_pH);
  Serial.print("Speed: "); 
  Serial.println(desired_speed);
}

void saveParameterEEPROM(int address, double parameter) {
  EEPROM.put(address, parameter);
}

void getDesiredParameters(int size) {
  if (size < 9) {
    Serial.println("Unidentified packet format! Trying again...");
    return;
  }
  
  uint8_t buffer[9];

  while (Wire.available()) {    
    for (int i = 0; i < size; i++) {
      buffer[i] = Wire.read();
    }

    uint8_t system_identifier = buffer[0];
    double parameter;
    memcpy(&parameter, &buffer[1], sizeof(double));

    switch (system_identifier) {
      case 0:
        desired_temp = parameter;
        saveParameterEEPROM(EEPROM_ADDRESS_TEMP, desired_temp);
        break;
      case 1:
        desired_pH = parameter;
        saveParameterEEPROM(EEPROM_ADDRESS_PH, desired_pH);
        break;
      case 2:
        desired_speed = parameter;
        saveParameterEEPROM(EEPROM_ADDRESS_SPEED, desired_speed);
        break;
      default:
        Serial.println("Invalid subsystem identifier!");
    }
  }
}

void writeCurrentParameters(double parameter, uint8_t identifier) {
  Wire.beginTransmission(ESP32_I2C_ADDRESS);
  uint8_t *bytes = (uint8_t *)(&parameter);
  Wire.write(identifier);
  Wire.write(bytes, sizeof(parameter));
  Wire.endTransmission();
}

void setup() {

  setupStirring();

  Wire.begin(ARDUINO_I2C_ADRESS);
  Wire.onReceive(getDesiredParameters);

  TCCR1A = 0b00000011; // 10 bit PWM
  TCCR1B = 0b00000001; // 7.8 kHz

  Serial.begin(9600);

  bool acknowledged = false;
  uint8_t ack[3] = {0};

  loadParameterEEPROM();
  while (!acknowledged) {
    Serial.println("Waiting for connection...");

    if (Wire.available() == 3) {
      for (int i = 0; i < 3; i++) {
        ack[i] = Wire.read();
      }

      if (ack[0] == 'a' && ack[1] == 'c' && ack[2] == 'k') {
        acknowledged = true;
        Serial.println("Connection established! System initialised.");
      }
    }

    delay(1000);
  }
}

void loop() {
  current_temp = runHeating(desired_temp);
  current_pH = runPH(desired_pH);
  current_speed = runStirring(desired_speed);

  unsigned long current_time = millis();

  if (current_time - previous_write_time >= WRITE_INTERVAL) {
    previous_write_time = current_time;

    writeCurrentParameters(current_temp, 0);
    writeCurrentParameters(current_pH, 1);
    writeCurrentParameters(current_speed, 2);
  }
}
