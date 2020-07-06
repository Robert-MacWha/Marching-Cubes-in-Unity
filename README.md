# Marching Cubes

An application of the marching cubes algorithm, a method used in creating 3D shapes, in the Unity Game Engine (https://unity.com/). 
Here are some examples of what it can produce:

![](https://github.com/Robert-MacWha/Marching-Cubes-in-Unity/blob/master/Sample%20Images/3D-1.PNG)
![](https://github.com/Robert-MacWha/Marching-Cubes-in-Unity/blob/master/Sample%20Images/Normal-2.PNG)
![](https://github.com/Robert-MacWha/Marching-Cubes-in-Unity/blob/master/Sample%20Images/Interpolated-1.PNG)
![](https://github.com/Robert-MacWha/Marching-Cubes-in-Unity/blob/master/Sample%20Images/Interpolated-2.PNG)

The algorithm works by creating a 3D array of densities (a float within the range [0-1]) and then sampling each density. Depending on whether the density is above or below a surface threshold. Then, a mesh (that can be split up into discrete chunks) is drawn around the surface.

Attached is a more detailed explanation of the implementation:
http://paulbourke.net/geometry/polygonise/

Initial project inspired by @SebLague
