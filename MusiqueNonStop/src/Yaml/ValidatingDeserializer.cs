/*
    Adds support for required properties to the YamlDotNet library's
    functionality. Based on a solution by rcdailey:
    https://github.com/aaubry/YamlDotNet/issues/202
*/

using System;
using System.ComponentModel.DataAnnotations;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NodeDeserializers;

namespace Kumodatsu.MusiqueNonStop.Yaml;

public class ValidatingDeserializer : INodeDeserializer {

    private readonly INodeDeserializer node_deserializer;

    public ValidatingDeserializer(INodeDeserializer node_deserializer)
        => this.node_deserializer = node_deserializer;

    public bool Deserialize(
        IParser                      parser,
        Type                         expected_type,
        Func<IParser, Type, object?> deserialize_nested_object,
        out object?                  value
    ) {
        if (
            !node_deserializer.Deserialize(
                parser, expected_type, deserialize_nested_object, out value
            ) || value is null
        ) {
            return false;
        }

        var context = new ValidationContext(value, null, null);

        try {
            Validator.ValidateObject(value, context, true);
        } catch (ValidationException exception) {
            if (parser.Current is null)
                throw;
            throw new YamlException(
                parser.Current.Start, parser.Current.End, exception.Message
            );
        }

        return true;
    }
}

public static class DeserializerBuilderExtension {

    public static DeserializerBuilder WithRequiredPropertyValidation(
        this DeserializerBuilder builder
    ) => builder.WithNodeDeserializer(
        inner => new ValidatingDeserializer(inner),
        s => s.InsteadOf<ObjectNodeDeserializer>()
    );

}
