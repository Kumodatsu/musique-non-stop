using CommandLine;

namespace Kumodatsu.MusiqueNonStop;

class CommandLineArgs {
    [Option('c', "config", Required = false,
        HelpText = "Path to the configuration file.")]
    public string ConfigPath { get; set; } = "config.yml";
}
