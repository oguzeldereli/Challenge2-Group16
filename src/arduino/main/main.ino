#include "PI_Motor_Control.h"
#include "PI_Heating_Control.h"
#include "PI_pH_Control.h"
#include <Wire.h>
#include <EEPROM.h>
#include "string.h"

#define SLAVE_ADDRESS 0x08
#define WRITE_INTERVAL 1000

#define EEPROM_ADDRESS_TEMP 0
#define EEPROM_ADDRESS_PH 8
#define EEPROM_ADDRESS_SPEED 16
#define I2C_BUFFER_SIZE 64

// Desired Parameters
double desired_speed = 800;
double desired_temp = 303;
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

void writeCurrentParameters(double parameter, uint8_t identifier) {
  uint8_t *bytes = (uint8_t *)(&parameter);
  Wire.write(identifier);
  Wire.write(bytes, sizeof(parameter));
}

bool isAck = false;
bool isResponseReady = false;
unsigned char responseDataBuffer[64];
unsigned char i2c_data_buffer[I2C_BUFFER_SIZE];
int responseLength = 0;
void onReceive(int length)
{
    int i = 0;
    unsigned char data;
    while (Wire.available() && i < I2C_BUFFER_SIZE && i < length)
    {
        data = Wire.read();
        i2c_data_buffer[i++] = data;
    }

    if(length == 1 && i2c_data_buffer[0] == 0xff)
    {
      memset(responseDataBuffer, 0, 64);
      responseDataBuffer[0] = 'A';
      responseDataBuffer[1] = 'C';
      responseDataBuffer[2] = 'K';
      responseLength = 3;
      isResponseReady = true;
      return;
    }
    else if(length == 1 && i2c_data_buffer[0] == 0x00)
    {
      memset(responseDataBuffer, 0, 64);
      responseDataBuffer[0] = 0;
      memcpy(responseDataBuffer + 1, &current_temp, sizeof(double));
      responseLength = 9;
      isResponseReady = true;
      return;
    }
    else if(length == 1 && i2c_data_buffer[0] == 0x01)
    {
      memset(responseDataBuffer, 0, 64);
      responseDataBuffer[0] = 1;
      memcpy(responseDataBuffer + 1, &current_pH, sizeof(double));
      responseLength = 9;
      isResponseReady = true;
      return;
    }
    else if(length == 1 && i2c_data_buffer[0] == 0x02)
    {
      memset(responseDataBuffer, 0, 64);
      responseDataBuffer[0] = 2;
      memcpy(responseDataBuffer + 1, &current_speed, sizeof(double));
      responseLength = 9;
      isResponseReady = true;
      return;
    }
    else if(length == 5)
    {
      uint8_t parameterIdentifier = i2c_data_buffer[0];
      double parameter;
      memcpy(&parameter, &i2c_data_buffer[1], sizeof(double));
      switch (parameterIdentifier) {
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
      return;
    }
}

void onRequest() 
{
  Serial.println("data reuqesteds");
  if (isResponseReady) 
  {
    Wire.write(responseDataBuffer, responseLength);
    isResponseReady = false;
  }
}

void setup() {
  setupHeating();
  setupPH();
  setupStirring();

  Wire.begin(SLAVE_ADDRESS);
  Wire.onReceive(onReceive);
  Wire.onRequest(onRequest);

  TCCR1A = 0b00000011; // 10 bit PWM
  TCCR1B = 0b00000001; // 7.8 kHz

  Serial.begin(250000);

  loadParametersEEPROM();
}

void loop() {
  current_temp = runHeating(desired_temp);
  current_speed = runStirring(desired_speed);

  unsigned long current_main_time = millis();
  
  if (current_main_time - previous_write_time >= WRITE_INTERVAL) {
    previous_write_time = current_main_time;

    current_pH = runPH(desired_pH);
  }
}