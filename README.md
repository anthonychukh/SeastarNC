# BeaverNC

Seastar, formerly known as Beaver is a library written for Rhino Common and Grasshopper for 3d printer control.


It support real-time connection between Grasshopper and open-source 3D printing firmware.
This project was made possible by the generous support of SPACE10 through a research resident program in summer 2019.
A protoype was created by modifying an Anycubic Linear Plus Delta printer with additional 3 rotational axes.


Seastar could be used for:
 * enabling the usage of 3D printers as robotic arm system.
 * connecting to other functions with grasshopper. eg. Firefly sensors.
 * custom tool path planning for 3d printing and milling.


There are 6 main catergory of component:
 * Config:   Read ini file from slicer software and extract existing parameter
 * Machine:  Create machines objects with information of dimension and rotational axes configuration
 * Path:	   Create path objects and provide means of manipulation. eg. join, insert	
 * File:     Save file for offline use
 * Control:  Real-time connection to 3d printer. Automatically queue and send command
 * Sim:      Visualization of tool path



-------------------DISCLAIMER--------------------------
This library and Grasshopper plugin was created out of good intension for the promotion and education of technology
User should take their own caution and risk when using this library
The creator(s) of this library do not provide any garantee and waranty to use of this library and digital tool derives from this library
  

--------------------CAUTION---------------------------
Working with electronic devices could be dangerous 
Always take precaution when working on electronic devices
Make sure you are well trained and informed to work on the specific system
Failure to appropriately operate your machine could lead to hardware damage and/or serious injuries