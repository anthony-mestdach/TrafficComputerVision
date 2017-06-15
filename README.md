# Computer Vision for Driver Assistance Systems
[![Emgu CV](https://avatars1.githubusercontent.com/u/2035816?v=3&s=40)](https://github.com/emgucv/emgucv)
## Overview
This repository contains basic applications for traffic sign recognition and lane detection. The code is written in C# .NET and uses Emgu CV (3.1.0.2504) as wrapper for OpenCV.

## TrafficSignRec
A SURF (Speeded up robust features) based detection and recognition tool. It uses the homography with RANSAC to increase stability. A set of known signs is matched with candidats in the image. 
![TrafficSignRec demo](https://github.com/anthony-mestdach/TrafficComputerVision/blob/master/Code/Results/TrafficSignRec/Afbeelding1.png)

## HaarCascadeDetector
A Haar based sign detector. Multiple Haar cascade files are included to detect a 90km speed limit sign (Belgian). 
![HaarCascadeDetector demo](https://github.com/anthony-mestdach/TrafficComputerVision/blob/master/Code/Results/HaarCascadeDetector/demo.png)

## Installation
