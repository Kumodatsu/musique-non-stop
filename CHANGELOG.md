# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog][changelog], and this project adheres
to [Semantic Versioning][semver].

## [Unreleased]
### Added
- Illustrative images in the readme file.
- If the configuration file doesn't exist yet, one of the correct format is now
automatically generated; the user only has to put their token in it.
- `ephemeral` key in the config file. This controls whether or not to delete the
music player message when leaving the voice channel.

### Changed
- Updated to Discord.NET version 3.1.0.
- The bot's interface now uses buttons and a thread for song requests, replacing
most slash commands.
- Commands that still exist now integrate with Discord's slash command system.
- The bot now needs both the `applications.commands` scope in addition to the
`bot` scope.
- The bot now needs the `Create Public Threads` and `Create Private Threads`
permissions.
- The bot no longer needs the `Embed Links` and `Read Message History`
permissions.
- Updated readme for the new changes.

### Removed
- `command-prefix` key in the config file. This no longer makes sense to have as
the commands are now integrated with Discord's slash command system.

## [0.1.1][] - 2022-01-04
### Added
- Releases will now include binaries for Windows, Linux and OSX target
platforms.

## [0.1.0][] - 2022-01-03
### Added
- Initial version of the bot that can use a [Lavalink][1] server to play
audio in a Discord voice chat. Includes the `ping`, `join`, `leave`, `play`,
`stop`, `skip`, `queue` and `help` commands.
- Changelog file.
- Readme file with build and usage instructions.

[1]: <https://github.com/freyacodes/Lavalink>

[changelog]: <https://keepachangelog.com/en/1.0.0/>
[semver]: <https://semver.org/spec/v2.0.0.html>

[0.1.0]: <https://github.com/Kumodatsu/musique-non-stop/releases/tag/v0.1.0>
[0.1.1]: <https://github.com/Kumodatsu/musique-non-stop/releases/tag/v0.1.1>
