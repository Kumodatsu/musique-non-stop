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
    public string Token     { get; set; } = "your token here";
    public bool   Ephemeral { get; set; } = false;

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

    public static void ToFile(Config config, string path) {
        using var writer = new StreamWriter(path);
        var serializer = new SerializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        serializer.Serialize(writer, config);
    }
}

public class ParseException : Exception {
    public ParseException(string message) : base(message) {}
}
