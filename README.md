# BuildLight
![Alt text](https://github.com/andy-kohne/BuildLight/raw/master/build_light_animated.gif "Animated Status Indicator")
![Alt text](https://github.com/andy-kohne/BuildLight/raw/master/RaspberryPi_with_board_small.jpg "Raspberry Pi with daughter board")
![Alt text](https://github.com/andy-kohne/BuildLight/raw/master/led_strips.jpg "LED strips")  

### Project Description
A Universal Windows Platform application intended to run under Windows IOT Core on a Raspberry Pi, driving external LED strips to indicate build / test status.

### Motivation
This project was born of frustration from a long run of getting the latest each morning only to find that it had been left broken the day before.  The idea is to have a visual indicator that is impossible to miss, tied to the build server, so that breaking commits can't fly under the radar.  Of course, it's also a good reason to play with a Pi.

### Details
This application polls a build server at a customizable interval.  When changes to the build status are found, the 'real world' visualization is updated to reflect the status change.  Multiple projects may be monitored, with separate or combined visualizations.  Any of the GPIO pins may be used, with pulse width modulation provided by the [Lightning Providers](https://docs.microsoft.com/en-us/windows/iot-core/develop-your-app/lightningproviders).

### Hardware
- A good 12V power supply
- 12V RGB LED Light Strip
- [Raspberry Pi 3 Model B](https://www.raspberrypi.org/products/raspberry-pi-3-model-b/)
- Daughter board
   + Converts 12V to 5V for powering the Pi
   + Drives the LED strips from the logic level outputs
