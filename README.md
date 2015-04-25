# PS Move Setup

### Introduction
PS Move Setup is a co-registration utility for the [PS Move](http://en.wikipedia.org/wiki/PlayStation_Move) + [PS Eye](http://en.wikipedia.org/wiki/PlayStation_Eye) using the [Oculus DK2](https://www.oculus.com/dk2/) tracker as a reference.

The goal is to show the PS Move controller in the same space referential than the Oculus DK2 to support 1:1 hand tracking in virtual reality applications.

### Video
[![PS Move Setup](http://img.youtube.com/vi/g05RT2GmZfE/0.jpg)](http://www.youtube.com/watch?v=g05RT2GmZfE)

### Description
It's written for Unity 5 and [cboulay](https://github.com/cboulay/psmoveapi) version of the PS Move API, which features a [new tracking algorithm](https://github.com/cboulay/psmove-ue4/wiki/Tracker-Algorithm) and an updated implementation of the [PS3EYEDriver](https://github.com/inspirit/PS3EYEDriver) driver for the PS Eye.

It's been developed for MS Windows but it should hopefully work on OS X. Linux support would probably require some code adaptation.

Previously the only driver available for MS Windows was the [CL Eye driver](https://codelaboratories.com/products/eye/driver/) but it was not free, unrealiable, it didn't support 64 bit applications and looked basically unmaintained.

Instructions to set up the PS Move and the PS Eye on MS Windows and OS X can be found here : [https://github.com/cboulay/psmove-ue4/wiki](https://github.com/cboulay/psmove-ue4/wiki)

### Implementation
The utility reads several hundreds positions of the PS Move and the Oculus DK2 (strapped together with rubber bands), stores them in a list of correlated positions and computes the rotation, translation and scale between the two sets of 3D points.

It's known as the [Wahba's problem](http://en.wikipedia.org/wiki/Wahba%27s_problem) and can be solved with the [Kabsch algorithm](http://en.wikipedia.org/wiki/Kabsch_algorithm).

The implementation uses the [Horn method](http://people.csail.mit.edu/bkph/papers/Absolute_Orientation.pdf) which is based on quaternions and seems to be more robust, precise and numerically stable than the [SVD method](http://nghiaho.com/?page_id=671).

A first step of outlier rejection is implemented using the [absolute deviation around the median](https://www.academia.edu/5324493/Detecting_outliers_Do_not_use_standard_deviation_around_the_mean_use_absolute_deviation_around_the_median).

The [Math.NET Numerics](http://numerics.mathdotnet.com/) library is used for the implementation of the Horn method (eigendecomposition of a matrix).

### Screenshots
![PS Move Setup in Unity editor](http://i.imgur.com/Hpx9GHQ.png)

![PS Move Setup in Unity](http://i.imgur.com/zRzwhYV.png)

![PS Move Setup detail](http://i.imgur.com/ZT0TEnF.png)

### Status
The utility is not complete yet. The registration code is functional and works correctly with test data, but the results are still unreliable when using the positions read from the PS Move. It's most probably because of noise or distortion in the tracking algorithm.

### Future
The goal is to reliably compute the rotation and translation between the two space referentials, obtain the corresponding transformation matrix and store it in a file in the `C:\Users\<Username>\AppData\Roaming\.psmoveapi` directory so it can be used directly by VR applications and games.

###Relevant discussions

* [Playstation Move controllers working reliably in UE4 in OSX and Windows. Now how to co-register with DK2?](http://www.reddit.com/r/oculus/comments/2z6dxa/playstation_move_controllers_working_reliably_in/) on reddit
* [Working PS Move plugin for UE4; need help coreg with DK2](https://forums.oculus.com/viewtopic.php?f=25&t=21371) on the Oculus VR Forums

**Note:** the Oculus Development Kit 2 3D model is from [MannyLectro](https://forums.oculus.com/viewtopic.php?t=1514).