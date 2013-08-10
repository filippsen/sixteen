sixteen
=======


What
----
This project consists of 16 games in 16KB of data, using Unity (web) engine (no streaming, self-contained data).
It was one of those Sunday midnight coding I run often times. This one was from mid 2012.


How
----
_Trivia :sunday night coding. Powered by Murphy's Irish Red Beer :) _

It leveraged the CHIP-8 virtual machine especification and its games.
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
* Check the original web version: http://filippsen.github.io/sixteen/sixteen.html

Controls:
|1 | 2 | 3 | 4 |
|Q | W | E | R |
|A | S | D | F |
|Z | X | C | V |


The project has been converted to Unity 4. The following releases were built with Unity 4.2 Pro Trial:
* Download Windows version: 
* Download Linux version: 
* Download Mac version: 


License
-------
Source code is released into the public domain. Read the LICENSE file.
All the binary files related to chip-8 (games located inside the Resources folder) are reportedly known to be placed in the public domain.
