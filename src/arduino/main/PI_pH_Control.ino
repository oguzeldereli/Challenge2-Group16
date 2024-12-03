#include <math.h>
#include <time.h>

// pumpA (acid) = red, pumpB (alkali) = black 
#define ph_inPin A2
#define ph_pumpAPin 6
#define ph_pumpBPin 5
#define error_margin 0.2

int total_seconds = 0;
double current_ph = 0;

double convertToPh(int analogInput) {
  return -0.0422 * analogInput + 15.7;
}

void setupPH() {
  pinMode(ph_pumpAPin, OUTPUT);
  pinMode(ph_pumpBPin, OUTPUT);
}

double runPH(double target_ph) {
  if (total_seconds >= 250) {
    analogWrite(ph_pumpAPin, 0);
    analogWrite(ph_pumpBPin, 0);
    Serial.println("pH control system paused, container is full");
  } else if (current_ph < target_ph + error_margin && current_ph > target_ph - error_margin) {
    analogWrite(ph_pumpAPin, 0);
    analogWrite(ph_pumpBPin, 0);
  } else {
    int analogInput = analogRead(ph_inPin);
    current_ph = convertToPh(analogInput);  

    if (current_ph < 3 || current_ph < target_ph) {
      // Too acidic, activate pumpB (alkali)
      analogWrite(ph_pumpAPin, 0);
      analogWrite(ph_pumpBPin, 255);
      total_seconds++;
    } else if (current_ph > 7 || current_ph > target_ph) {
      // Too alkaline, activate pumpA (acid)
      analogWrite(ph_pumpAPin, 0);
      analogWrite(ph_pumpBPin, 255);
      total_seconds++;
    } else {
      analogWrite(ph_pumpAPin, 0);
      analogWrite(ph_pumpBPin, 0);
    }
  }

  return current_ph;
}