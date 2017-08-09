using Runtime;
using LogForNet;
using NewtonSoft;

namespace Application {
    using static Extensions;

    instance ISerializableJson : ISerializable<Json> {
        string Serialize(Json json) => json.ToString();
        Json DeSerialize(string Json) => new Json();
    }
    public class Application   {
        public static void Main() {
            var json = new Json();
            var s = json.Serialize();
            var _ = s.DeSerialize<Json,ISerializableJson>();
            //BUG: we should also be able to just write:
            //var _ = s.DeSerialize<Json>();
            //inferring the second instance type parameter
        }
    }
}
