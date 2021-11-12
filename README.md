# VATSIM Live Feedback

This project aims to provide an interactive and easy-to-use way for giving feedback to each other
while flying online in the VATSIM network.

## Project Goals

Current solutions for feedback on VATSIM all have their weaknesses:

- Pilots and controllers have the possibility to give feedback to each other via radio. This ranges
  from a short "thank you for the service" to - sometimes emotionally - expressed displeasure. This
  type of feedback is hardly possible when the frequency is heavily loaded; however, it is particularly
  under these circumstances where situations arise in which one would like to both motivate or criticize.

- Pilots as well as controllers can use (private) text messages in the client software to provide
  asynchronous feedback. This is often cumbersome and can also distract from more important events, as
  client software prioritizes all messages equally. If you want to write a private message at a later
  time, you might not remember the callsign, or the controller may already have gone offline.

- Ticket portals such as https://feedback.vatsim-germany.org give the impression of being mainly for
  negative feedback. They also suffer from the delay: after the flight, you might not remember the name
  of the controller you were dealing with.

- Feedback on publicly accessible bulletin boards regularly leads to debates about fundamental issues.

This project attempts to solve these problems: Pilots are given a simple way to list all interactions
of their flight in real-time and rate them individually or collectively. This could include simple reaction
emojis, but also free text. Bashing and non-constructive feedback will be reduced by using authentication
via https://auth.vatsim.net: Users can block each other if they receive unwanted messages. Furthermore,
controllers will have the possibility to reply feedback.

## Contributing

Both pull requests and contribution to issues are welcome. For major ideas or changes, please open an issue
first to discuss what you would like to change.

Please make sure to update tests as appropriate.

### Prerequisites

You'll need to install the following pre-requisites in order to build this application:

* [.NET Core SDK](https://www.microsoft.com/net/download) 5.0 or higher
* [Node LTS](https://nodejs.org/en/download/)

### Starting the application

To concurrently run the server and the client components in watch mode use the following command:

```bash
dotnet run
```

Then open `http://localhost:1234` in your browser.

### Build targets

The build project in root directory contains a couple of different build targets. You can specify them after `--` (target name is case-insensitive).

To run concurrently server and client tests in watch mode (you can run this command in parallel to the previous one in new terminal):

```bash
dotnet run -- RunTests    # Currently inoperative due to switch from Webpack to Parcel (PR welcome!)
```

Client tests are available under `http://localhost:8081` in your browser and server tests are running in watch mode in console.

Finally, there are `Bundle` and `Azure` targets that are used to package the app and deploy to Azure, respectively:

```bash
dotnet run -- Bundle
dotnet run -- Azure
```

### SAFE Stack Documentation

If you want to know more about the full Azure Stack and all of it's components (including Azure) visit the official [SAFE documentation](https://safe-stack.github.io/docs/).

You will find more documentation about the used F# components at the following places:

* [Saturn](https://saturnframework.org/)
* [Fable](https://fable.io/docs/)
* [Elmish](https://elmish.github.io/elmish/)
