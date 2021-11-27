using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Kumodatsu.MusiqueNonStop.Yaml;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Kumodatsu.MusiqueNonStop;

public class Config {
    [Required]
    public string Token         { get; set; } = "";
    public string CommandPrefix { get; set; } = "//";

    public static Config FromFile(string path) {
        using var reader = new StreamReader(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .WithRequiredPropertyValidation()
            .Build();
        Config config;
        try {
            config = deserializer.Deserialize<Config>(reader);
        } catch (YamlException exception) {
            throw new ParseException(exception.Message);
        }
        return config;
    }
}

public class ParseException : Exception {
    public ParseException(string message) : base(message) {}
}
