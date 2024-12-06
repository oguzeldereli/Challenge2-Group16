#define ph_inPin A2
#define ph_pumpAPin 6
#define ph_pumpBPin 5
#define error_margin 0.2
#define pump_run_duration 5

int current_volume = 0;
double current_ph = 0, previous_ph = 0;
int elapsed_seconds = 0;  // Tracks the total runtime in seconds
int pump_active_time = 0; // Tracks the last time the pump was activated
bool pump_active = false; // Tracks whether a pump is currently active

double convertToPh(int analogInput) {
  return -0.0422 * analogInput + 15.7;
}

void setupPH() {
  pinMode(ph_pumpAPin, OUTPUT);
  pinMode(ph_pumpBPin, OUTPUT);
  analogWrite(ph_pumpAPin, 0);
  analogWrite(ph_pumpBPin, 0);
}

double runPH(double target_ph) {
  // Increment elapsed time
  elapsed_seconds++;

  if (current_volume >= 250) {
    analogWrite(ph_pumpAPin, 0);
    analogWrite(ph_pumpBPin, 0);
    pump_active = false;
    Serial.println("pH control system paused, container is full");
    return current_ph;
  }

  // Check if the pH is within the acceptable range
  if (abs(current_ph - target_ph) <= error_margin) {
    analogWrite(ph_pumpAPin, 0);
    analogWrite(ph_pumpBPin, 0);
    pump_active = false;
    return current_ph;
  }

  // Read the current pH level
  int analogInput = analogRead(ph_inPin);
  current_ph = convertToPh(analogInput);

  // Stop pump if it's been running for 5 seconds
  if (pump_active && (elapsed_seconds - pump_active_time >= pump_run_duration)) {
    analogWrite(ph_pumpAPin, 0);
    analogWrite(ph_pumpBPin, 0);
    pump_active = false;
    return current_ph; // Wait until the next call for further action
  }

  // Check if the pH has changed significantly since the last measurement
  if (!pump_active && abs(current_ph - previous_ph) < error_margin) {
    return current_ph;
  }

  // Activate the appropriate pump based on the current pH level
  if (!pump_active) {
    if (current_ph < target_ph) {
      // Too acidic, activate pumpB (alkali)
      analogWrite(ph_pumpAPin, 0);
      analogWrite(ph_pumpBPin, 255);
    } else if (current_ph > target_ph) {
      // Too alkaline, activate pumpA (acid)
      analogWrite(ph_pumpAPin, 255);
      analogWrite(ph_pumpBPin, 0);
    }
    previous_ph = current_ph;
    pump_active = true;
    pump_active_time = elapsed_seconds;
    current_volume++;
  }

  Serial.print("Current pH: ");
  Serial.println(current_ph);
  return current_ph;
}
