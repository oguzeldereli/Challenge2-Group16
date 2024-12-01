#include "PI_Motor_Control.h"

void addSpeedToBuffer(float speed) {
  speedBuffer[bufferIndex] = speed;
  bufferIndex = (bufferIndex + 1) % filterSize;
}

float getFilteredSpeed() {
  float sum = 0;
  for (int i = 0; i < filterSize; i++) {
    sum += speedBuffer[i];
  }
  return sum / filterSize;
}

void setupStirring() {
  pinMode(input_pin, INPUT_PULLUP); // Configure input pin with pull-up resistor
  pinMode(output_pin, OUTPUT); // Configure output pin for PWM
}

float runStirring(float desired_speed) {
  unsigned long current_time = micros(); // Use micros() for higher-resolution timing

  // Check the sensor input for rising edges
  static bool last_state = HIGH;
  bool current_state = digitalRead(input_pin);

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
      float time_elapsed = (current_time - pulse_start_time) / 1000000.0; // Elapsed time in seconds
      motor_speed = (pulse_count / pulses_per_revolution) / time_elapsed * 60.0; // RPM
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

  // PI Control every control_interval microseconds
  if (current_time - control_previous_time >= control_interval) {
    control_previous_time = current_time;

    // Calculate the error
    error = desired_speed - motor_speed;

    // Accumulate the error for the Integral term
    sum_error += error * (control_interval / 1000000.0); // Scale by interval time in seconds

    // Compute the control signal
    Vmotor = Kp * error + Ki * sum_error;

    // Constrain the PWM signal to valid range
    Vmotor = constrain(Vmotor, 0, 1023);

    // Write the control signal to the motor
    analogWrite(output_pin, Vmotor);
  }

  return motor_speed;
}