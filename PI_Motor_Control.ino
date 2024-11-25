const int input_pin = 2; // Sensor input pin
const int output_pin = 10; // PWM output pin
const int pulses_per_revolution = 70; // Pulses per motor revolution

unsigned long previous_time = 0; // For timing speed measurements 
unsigned long pulse_start_time = 0; // Time of the first pulse in a measurement period
unsigned int pulse_count = 0; // Number of pulses counted in a period
float motor_speed = 0; // Motor speed in RPM

// Motor Constants
const float T = 0.15;
const float Z = 1;
const float wn = 6.67;
const float Kv = 225;
const float wo = 1/T;

// PI Control Variables
float Kp = (2 * Z * wn / wo - 1) / Kv; // Proportional gain
float Ki = wn * wn / Kv / wo;  // Integral gain
float desired_speed = 800.0; // Desired motor speed in RPM
float error = 0; // Current error
float sum_error = 0; // Accumulated error (for Integral term)
float Vmotor = 0; // Control signal (PWM value)

// Timing for control loop
unsigned long control_previous_time = 0;
const unsigned long control_interval = 10000; // Control interval in microseconds (10 ms)

void setup() {
  pinMode(input_pin, INPUT_PULLUP); // Configure input pin with pull-up resistor
  pinMode(output_pin, OUTPUT); // Configure output pin for PWM

  TCCR1A = 0b00000011; //10 bit
  TCCR1B = 0b00000001; // 7.8 kHz

  Serial.begin(250000); // Start serial communication for debugging
}

void loop() {
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

  // Calculate motor speed every second (1,000,000 microseconds)
  if (current_time - previous_time >= 1000000) {
    if (pulse_count > 0) {
      float time_elapsed = (current_time - pulse_start_time) / 1000000.0; // Elapsed time in seconds
      motor_speed = (pulse_count / pulses_per_revolution) / time_elapsed * 60.0; // RPM
    } else {
      motor_speed = 0; // No pulses detected
    }

    pulse_count = 0; // Reset pulse count
    previous_time = current_time; // Update timing

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

    //Serial.print("Control Signal (Vmotor): ");
    //Serial.println(Vmotor);
  }
}