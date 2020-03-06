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

# License

MIT

# Assessment

The code works, but a lot of things are still hardcoded. Needs better configuration files, a Gui for configuration and some work on the Gui in general.

Right now the file size that 

# Plans

- Integratie into the main LPG branch

# Acknowledgements

This software was developed from 2019 to 2020  at

__Berner Fachhochschule - Labor f�r Photovoltaik-Systeme__

Part of the Development was funded by the

__Swiss Federal Office of Energy__

This happend in the project "SimZukunft".

I am very grateful for the support!