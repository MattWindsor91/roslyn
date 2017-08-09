namespace Runtime {
    public concept ISerializable<T>  {
        string Serialize(T t);
        T DeSerialize(string t);
    }
    public static class Extensions {
        public static string Serialize<T>(this T This) where E : ISerializable<T>
            => E.Serialize(This);
        public static T DeSerialize<T>(this string This) where E : ISerializable<T>
           => E.DeSerialize(This);
    }
}
