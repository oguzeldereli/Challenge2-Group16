#include "PI_Motor_Control.h"

#define STIRRING_INPUT_PIN 2 // Sensor input pin
#define STIRRING_OUTPUT_PIN 11 // PWM output pin
#define PULSES_PER_REVOLUTION 70 // Pulses per motor revolution


// Motor Constants
const double T = 0.115;
const double Z = 0.75;
const double wn = 4.5;
const double Kv = 225;
const double wo = 1/T;

unsigned long previous_time = 0; // For timing speed measurements 
unsigned long pulse_start_time = 0; // Time of the first pulse in a measurement period
unsigned int pulse_count = 0; // Number of pulses counted in a period
double motor_speed = 0; // Motor speed in RPM

// PI Control Variables
double Kp = (2 * Z * wn / wo - 1) / Kv; // Proportional gain
double Ki = 1.25 * (wn * wn / Kv / wo);  // Integral gain

// PI Error Values
double error = 0; // Current error
double sum_error = 0; // Accumulated error (for Integral term)
double Vmotor = 0; // Control signal (PWM value)

// Timing for control loop
unsigned long control_previous_time = 0;
const unsigned long CONTROL_INTERVAL = 1000; // Control interval in microseconds (1 ms)

// Damping Variables
#define FILTER_SIZE 10
double speedBuffer[FILTER_SIZE];
int bufferIndex = 0;

void addSpeedToBuffer(double speed) {
  speedBuffer[bufferIndex] = speed;
  bufferIndex = (bufferIndex + 1) % FILTER_SIZE;
}

double getFilteredSpeed() {
  double sum = 0;
  for (int i = 0; i < FILTER_SIZE; i++) {
    sum += speedBuffer[i];
  }
  return sum / FILTER_SIZE;
}

void setupStirring() {
  pinMode(STIRRING_INPUT_PIN, INPUT_PULLUP); // Configure input pin with pull-up resistor
  pinMode(STIRRING_OUTPUT_PIN, OUTPUT); // Configure output pin for PWM
}

double runStirring(double desired_speed) {
  unsigned long current_time = micros(); // Use micros() for higher-resolution timing

  // Check the sensor input for rising edges
  static bool last_state = HIGH;
  bool current_state = digitalRead(STIRRING_INPUT_PIN);

  if (last_state == LOW && current_state == HIGH) { // Rising edge detected
    if (pulse_count == 0) {
      pulse_start_time = current_time; // Record the time of the first pulse
    }
    pulse_count++;
  }
  last_state = current_state;

  // Calculate motor speed every 1/10th of a second (1,000,000 microseconds)
  if (current_time - previous_time >= 1000000) {
    if (pulse_count > 0) {
      double time_elapsed = (current_time - pulse_start_time) / 1000000.0; // Elapsed time in seconds
      motor_speed = (pulse_count / PULSES_PER_REVOLUTION) / time_elapsed * 60.0; // RPM
    } else {
      motor_speed = 0; // No pulses detected
    }

    pulse_count = 0; // Reset pulse count
    previous_time = current_time; // Update timing

    addSpeedToBuffer(motor_speed);
    motor_speed = getFilteredSpeed();

    Serial.print("Motor Speed:");
    Serial.print(motor_speed);
    Serial.println(" RPM");
  }

  // PI Control every CONTROL_INTERVAL microseconds
  if (current_time - control_previous_time >= CONTROL_INTERVAL) {
    control_previous_time = current_time;

    // Calculate the error
    error = desired_speed - motor_speed;

    // Accumulate the error for the Integral term
    sum_error += error * (CONTROL_INTERVAL / 1000000.0); // Scale by interval time in seconds

    // Compute the control signal
    Vmotor = Kp * error + Ki * sum_error;

    // Constrain the PWM signal to valid range
    Vmotor = constrain(Vmotor, 0, 1023);

    // Write the control signal to the motor
    analogWrite(STIRRING_OUTPUT_PIN, Vmotor);
  }

  return motor_speed;
}