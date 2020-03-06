# DistributedSimulator

This came out of a research project that analyzed the low voltage grid in the Swiss town of Burgdorf. It is a distributed job scheduler for the LoadProfileGenerator (www.loadprofilegenerator.de)

This is a very early version and is only useable for developers. It still contains a lot of hardcoded paths and assumptions from the research project.

The purpose for this is to serve as a foundation for future development. It could be useable for others with a little work.

What it does is the following:
- You start the server on a central computer. 
- Then you put a ton of house jobs (think >>1000, otherwise not worth the effort) into a folder. 
- Then you start the client on as many computers as you can get your hand on. 
- The clients will fetch calculation jobs from the server, execute them and send back the resulting directory contents.
- The server saves the contents to a folder.

# Technology

- C#
- WPF
- Windows Only
- ZeroMQ
- MessagePack for packing and zipping

# License

MIT

# Assessment

The code works, but a lot of things are still hardcoded. Needs better configuration files, a Gui for configuration and some work on the Gui in general.

Right now the total size of the files that can be sent back is limited to less than about 1 GB. More than that seems to frequently mess up ZeroMQ. This could easily be fixed
by implmenting a chunking mechanism, but I never got around to that since 1GB was plenty for my application.

# Plans

- Integrate into the main LPG branch
- Port to Linux with .NET Core 3

# Acknowledgements

This software was developed from 2019 to 2020  at

__Berner Fachhochschule - Labor f�r Photovoltaik-Systeme__

Part of the Development was funded by the

__Swiss Federal Office of Energy__

This happend in the project "SimZukunft".

I am very grateful for the support!