#ifndef PI_MOTOR_CONTROL_H
#define PI_MOTOR_CONTROL_H

const int input_pin = 2; // Sensor input pin
const int output_pin = 10; // PWM output pin
const float pulses_per_revolution = 70; // Pulses per motor revolution

unsigned long previous_time = 0; // For timing speed measurements 
unsigned long pulse_start_time = 0; // Time of the first pulse in a measurement period
unsigned int pulse_count = 0; // Number of pulses counted in a period
float motor_speed = 0; // Motor speed in RPM

// Motor Constants
const float T = 0.115;
const float Z = 0.8;
const float wn = 15;
const float Kv = 225;
const float wo = 1/T;

// PI Control Variables
float Kp = (2 * Z * wn / wo - 1) / Kv; // Proportional gain
float Ki = wn * wn / Kv / wo;  // Integral gain

// PI Error Values
float error = 0; // Current error
float sum_error = 0; // Accumulated error (for Integral term)
float Vmotor = 0; // Control signal (PWM value)

// Timing for control loop
unsigned long control_previous_time = 0;
const unsigned long control_interval = 1000; // Control interval in microseconds (1 ms)

// Damping Variables
const int filterSize = 5;
float speedBuffer[filterSize];
int bufferIndex = 0;

// I2C Constants
const int esp32_address;

void addSpeedToBuffer(float speed);
float getFilteredSpeed();
void setupStirring();
float runStirring(float desired_speed);

#endif