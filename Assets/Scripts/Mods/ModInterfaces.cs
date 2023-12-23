using Newtonsoft.Json.Linq;

namespace GameCore
{
    public interface IJsonFormat
    {
        string jsonFormat { get; set; }
    }

    public interface IJsonFormatWhenLoad
    {
        string jsonFormatWhenLoad { get; set; }
    }

    public interface IJObject
    {
        JObject jo { get; set; }
    }

    public interface IJToken
    {
        JToken jt { get; set; }
    }

    public interface IJOFormatCore : IJsonFormat, IJsonFormatWhenLoad, IJObject
    {

    }

    public interface IJOFormatCoreChild : IJsonFormat, IJsonFormatWhenLoad, IJToken
    {

    }
}