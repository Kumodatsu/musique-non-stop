# Musique Non Stop
Yet another Discord music bot.

## Getting started
All paths and commands presented in this section are relative to the
repository's root folder (that is, the folder that contains this README file)
unless otherwise specified.

### Prerequisites
To run this project you need the following:
- To build the source code, you need [.NET SDK version 6.0.0][1] or greater. If
you don't want to build the code yourself (or don't know what that means), you
can also simply [download the program][5].
- You need a Discord bot token. If you don't have one yet, [create a new
Discord application][2] and add a bot to it.
- For the bot to play any audio, you need to run a [Lavalink][3] server.
Lavalink itself requires Java. See its documentation on how to set up
(especially the "Server configuration" section in its README).
- You need to make a configuration file as described in the following section.

### Configuration
The bot requires a configuration with certain information to exist. The
configuration file must be a [YAML][4] file containing the following keys:

Key            | Description
---------------|-------------
token          | Your bot's token.
command-prefix | The desired prefix for the bot's commands.

Example of a valid configuration file:

```yml
token: your bot's token here
command-prefix: //
```

### Building
Skip this part if you downloaded the program.

Run the following command from the `MusiqueNonStop` folder:

    dotnet build -c <CONFIGURATION>

where `<CONFIGURATION>` is one of `Debug`, `Release` depending on which
configuration you want to build.

### Running
To use the bot's audio functionality you must have a Lavalink server set up and
running (see the [prerequisites](#prerequisites)). Once this is done, run the
following command from the `MusiqueNonStop` folder:

    dotnet run -c <CONFIGURATION> -- <ARGS>

where `<CONFIGURATION>` is one of `Debug`, `Release` depending on which
configuration you want to run; and `<ARGS>` are the command line arguments for
the program. The supported commands are:

Command     | Description
------------|------------
`-c <PATH>` | The path to your config file. Defaults to `config.yml`.
`--help`    | Displays a list of all command line arguments.
`--version` | Displays information about the program's version.

## Adding the bot to a server
Find your bot's Application ID. You can find this in your application's page
under the tab "General Information". In the following link, replace
`<APPLICATION_ID>` with your Application ID:

    https://discord.com/api/oauth2/authorize?client_id=<APPLICATION_ID>&permissions=274881137728&scope=bot

Then open the resulting link in your web browser. This allows you to add the bot
to a server on which you have the appropriate permissions, giving the bot the
minimal permissions it needs to function. The bot uses the following
permissions:

- Read Messages/View Channels
- Send Messages
- Send Messages in Threads
- Embed Links
- Read Message History
- Add Reactions
- Connect
- Speak

## Usage
The commands listed in this section assume the command prefix (as set in the
configuration) to be `//`; if you specified a different prefix, use that
instead. Commands must be sent in a text channel that the bot can read, and some
commands (notably the ones affecting the audio playing) require you to be in a
voice chat that it can join.

Command           | Description
------------------|------------
`//help`          | Displays the list of commands. 
`//ping`          | Triggers a response from the bot. Useful to test latency.
`//join`          | Makes the bot join your voice channel.
`//leave`         | Makes the bot leave its voice channel.
`//play <QUERY>`  | Plays a song from a given query (see below).
`//stop`          | Stops the currently playing song.
`//skip`          | Skips the currently playing song.
`//queue`         | Shows the current song queue.

Queries must be either a direct YouTube URL to a video with the desired audio,
or any text to search for on YouTube; in the latter case, the first result will
be selected.

[1]: <https://dotnet.microsoft.com/download/dotnet/6.0>
[2]: <https://discord.com/developers/applications>
[3]: <https://github.com/freyacodes/Lavalink>
[4]: <https://yaml.org/>
[5]: <https://github.com/Kumodatsu/musique-non-stop/releases/latest>
