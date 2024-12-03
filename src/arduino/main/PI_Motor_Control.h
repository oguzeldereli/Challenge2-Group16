#ifndef PI_MOTOR_CONTROL_H
#define PI_MOTOR_CONTROL_H

void addSpeedToBuffer(double speed);
double getFilteredSpeed();
void setupStirring();
double runStirring(double desired_speed);

#endif