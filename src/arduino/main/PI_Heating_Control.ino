int decimalPrecision = 2;               
                
double voltageDividerR1 = 220;                            
double R1 = 10000;                                           
double R2 ;                              
double T2 ;                                              

double tempSampleRead  = 0;               
double tempLastSample  = 0;               
double tempSampleSum   = 0;               
double tempSampleCount = 0;               
double tempMean ; 

 // declare constants and variables
const byte tempsetpin = A0, tempsensorpin = A1, PIoutpin = 10;
const double ki = 0.0531, kp = 300; // assumes PIoutpin (0-1023 PWM) scales to 0 - 1023 W
const double Tcal = 0.1; // assumes 20.5 K/V temp sensor (Tcal = 20.5*5/1023)
long currtime, prevtime;
double deltaT,  Te,  TeInt;
int Pheater;

const double tempMin = 25.0;
const double tempMax = 35.0;
                 
    
void setupHeating() {
  pinMode(tempsetpin, INPUT);
  pinMode(tempsensorpin, INPUT);
  pinMode(PIoutpin, OUTPUT);
}

double runHeating(double T1) {
  //temperature measuring
  if(millis() >= tempLastSample + 1) {
    tempSampleRead = analogRead(tempsensorpin);                                                 
    tempSampleSum = tempSampleSum+tempSampleRead;                                               
    tempSampleCount = tempSampleCount+1;                                                        
    tempLastSample = millis();                                                                  
  }

  if(tempSampleCount == 1000) {
    tempMean = tempSampleSum / tempSampleCount;                                                 
    R2 = (voltageDividerR1*tempMean)/(1023-tempMean);                                           
    T2 = -0.0668*pow(R2/1000,3) + 2.2444*pow(R2/1000,2) - 26.571*(R2/1000) + 133.33;
                                                                                                       
    Serial.print(T2,decimalPrecision);                                                 
    Serial.println(" °C"); 
            
    tempSampleSum = 0;                                                                         
    tempSampleCount = 0; 
  }
    

  //heating 
  currtime = micros();
  deltaT = (currtime - prevtime)*1e-6; // measure deltaT for integration
  prevtime = currtime;
    
  if (T2 < tempMin) {
    Te = Tcal*(analogRead(tempsetpin)-analogRead(tempsensorpin));
    //analogue value at 25 degree is 1000-1007
    TeInt = TeInt + Te*deltaT; // integral of Te
    TeInt =  constrain(TeInt,0, 19266);
    Pheater = round(kp*Te+ki*TeInt);
    Pheater = constrain (Pheater,0, 1023);
    analogWrite(PIoutpin, 255); // where 0 < Pheater < 1023
  } else if (T2 >= tempMax){
    analogWrite(PIoutpin, 0);
    TeInt = 0;
  } 

  return T2;
}
