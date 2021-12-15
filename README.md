# Twitter_Clone
Implemented a Twitter clone engine with a basic UI using an Actor-model based client simulator.

# Team Members
1.	Akshay Ganapathy (UFID - 3684-6922)
2.	Kamal Sai Raj Kuncha (UFID - 4854-8114)

# Objective
The objective of this project is to implement a Web Socket interface to the Project 4 (Part-1) which was the implementation of Twitter clone engine and an Actor - model based client simulator.
Zip File Contents
The zip file consists of the readme.md file, Project_Report.pdf file, Proj4_2.fsx file, Proj4_2.fsproj file, and the client/index.html file, which contains the code to be run and the demo.mp4 file, which is the demo video of the project.

# Demo Video
The demo video (demo.mp4) is present inside the zip file.

# How To Run
•	Open the terminal and execute the command "dotnet run” which starts the server. Make sure the .fsproj file is present in the same directory in which you are executing the command.
•	Open the file index.html (UI), which contains the client code in any browser.
•	Use the User Interface to register, login, logout, tweet, follow a user, get tweets, get mentions, and get hashtags.

# Languages used
F# and JavaScript was used to code the project.

# Platforms used for running the code
Visual Studio Code
.NET version 5.0
NuGET Akka.NET v1.4.25

# What is Working
Basic Requirements - Completed

# Implementation
This project mainly consists of the following parts:

Backend
•	The backend code contains all the functions or methods for all the activities present in the twitter application user interface. When the user tries to login or logout or get mentions or hashtags etc., the button press event is triggered and then depending on which activity is requested, acts according to the functions written in the server side to give the user the requested result.

Web Sockets
•	These are connections that make it viable to establish a two-way interactive channel between the client and the server. So, when the server is running and a client logs into the server, we can see that a webSocket connection is established with the address followed by /websocket URL. We can observe this in the mainConnect module of the code.
•	After the connection between client and server is established, the initial handshake messages are exchanged between the client and the server. The server then receives the username from the client and then the server maintains a map object storing the list of all usernames mapped to the client reference ID.
•	To push any updates to the current client, the corresponding actor exchanges the messages and updates through the webSocket address associated with this client.
•	Suppose if a client receives any message from the server through the webSocket, it is displayed on the live feed in the UI where the client can see it.

REST APIs
•	The REST implementation for this project has been done using the Suave library of F#.
•	GET requests – get tweets, mentions and hashtags.
•	POST requests to the server – tweet, register, logout and login requests from the user. Each activity is a method in the code.
•	Upon receiving a request, the server performs the respective functions by assigning work to several actors as per the client’s request.
•	All the REST API calls can be found in the routingFunctions module of the code.

