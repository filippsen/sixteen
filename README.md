sixteen
=======


What
----
This project consists of 16 games in 16KB of data, using Unity (web) engine (no streaming, self-contained data).

It was one of those Sunday midnight coding I run often times. This one was from mid 2012.

![Screenshots 1](/ss_1.png "Screenshots 1")

How
----

It leverages the CHIP-8 virtual machine especification and its games.
I implemented the emulator for interpreting CHIP-8 games and shrank it down to 16KB.
I used Unity 3.4 back then. At that time I was really aware of Unity web engine footprint so I thought I could shoot for 16KB.
There was still a corcern about the sound effects though, mostly because of the lack of low-level sfx access on there.
I managed to make a little silly sound fit into the build at the end.


I also remeber doing code size optimizations. I frequently analyzed and compared different dissembly outputs to decrease code size of certain IL code chunks.
The tool used was most probably the Microsoft NET Framework IL Disassembler.


The final unity3d files was 15,952 bytes.


On the controls side of things, it was weird. Weird mostly because the machine input is based on a hex keyboard. 
I ended up mapping it to the folling keyboard layout:

|1 | 2 | 3 | 4 |

|Q | W | E | R |

|A | S | D | F |

|Z | X | C | V |



Downloads
--------
* Check out the original web version: http://filippsen.github.io/sixteen/sixteen.html
* Download the original web version (27 KB): https://github.com/filippsen/sixteen/releases/download/original-web/sixteen-web.zip

Controls:

|1 | 2 | 3 | 4 |

|Q | W | E | R |

|A | S | D | F |

|Z | X | C | V |



The project has been converted to Unity 4.
* Download Windows version (7.29 MB): 
* Download Linux version (17.3 MB): 
* Download Mac version (20.47 MB): 


License
-------
Source code is released into the public domain. Read the LICENSE file.

All the binary files related to CHIP-8 (games located inside the Resources folder) are reportedly to be placed in the public domain.
