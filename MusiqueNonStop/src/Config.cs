using System.IO;
using Newtonsoft.Json;

namespace Kumodatsu.MusiqueNonStop {

    record Config(
        [JsonProperty("token")]          string Token,
        [JsonProperty("command_prefix")] string CommandPrefix
    ) {
        public static Config? FromFile(string path) {
            using var reader = new StreamReader(path);
            return JsonConvert.DeserializeObject<Config>(reader.ReadToEnd());
        }
    }

}
