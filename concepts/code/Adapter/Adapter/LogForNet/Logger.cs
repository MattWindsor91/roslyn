using Runtime;

namespace LogForNet {
    public static class Logger {
        public static void Log<T>(T t) where _ : ISerializable<T> {
            System.Console.WriteLine(Serialize(t));
        }
    }
}
