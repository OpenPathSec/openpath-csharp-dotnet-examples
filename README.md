# Openpath C#/.NET sample code

This repository contains sample code for using C# in a .NET
environment to interact with Openpath APIs and systems.

> For general info about Openpath, see https://www.openpath.com/.

> For developer-centric info, including walk-throughs on using the API
> as well as detailed interactive API documentation, see
> https://openpath.readme.io/.

Note that the sample code here is merely intended to demonstrate the
minimum sequence of steps needed to get you up and running with a data
flow, and is such is written in a straightline fashion, without the
encapsulation, abstractions, error-handling, or separation of
code/config that you would expect in production code.

### MQTT/websockets sample

For general info on our MQTT system for pushing real-time events out
to clients, see
https://openpath.readme.io/docs/real-time-events-via-mqtt.

This example relies on [MQTTnet, an MQTT
library](https://github.com/chkr1011/MQTTnet) as well as [Json.NET,
for JSON parsing](https://www.newtonsoft.com/json).

See the example code in [Program.cs](MqttExample/Program.cs).

To use:
- open [MqttExample.sln](MqttExample/MqttExample.sln) in Visual Studio
- build (Build > Build Solution, or Ctrl+Shift+B)
- open a shell to run the program interactively (Tools > Command Line > Developer Command Prompt)
- then run as follows
  ```
  bin\Debug\MqttExample.exe you@corp.com yourOpenpathPassword yourOpenpathOrgId yourOpenpathAcuId
  ```

The program will perform the following sequence:
1) form auth header from username+password
2) call Openpath API to get mqtt credentials info
3) connect to AWS mqtt broker using URL from response (2)
4) subscribe to the mqtt topics indicated in (2)
5) call Openpath API again to force an ACU to send an mqtt message
   (basically the same kind of message that gets sent in response to
   door unlocks or open/close or other ACU events)
6) within a few seconds should receive and print the message(s) that
   are received in response to (6)
7) if you leave it running, it will continue to show additional
   messages that might be received, so you can test by sending some
   unlocks to the ACU which should result in new messages showing up
