using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
///     Encoding of the SimpleJSON example from Real-World Haskell:
///     http://book.realworldhaskell.org/read/using-typeclasses.html
/// </summary>
namespace RWHSimpleJson
{
    /// <summary>
    ///     A JSON value.
    /// </summary>
    interface IJValue
    {
        string Render();
    }

    /// <summary>
    ///     A JSON string.
    /// </summary>
    struct JString : IJValue
    {
        public string String;

        public string Render() => $"\"{String}\"";
    }

    /// <summary>
    ///     A JSON number.
    /// </summary>
    struct JNumber : IJValue
    {
        public double Number;

        public string Render() => Number.ToString();
    }

    /// <summary>
    ///     A JSON Boolean.
    /// </summary>
    struct JBool : IJValue
    {
        public bool Bool;

        public string Render() => Bool ? "true" : "false";
    }

    /// <summary>
    ///     A JSON null literal.
    /// </summary>
    struct JNull : IJValue
    {
        public string Render() => "null";
    }

    /// <summary>
    ///     A JSON object.
    /// </summary>
    struct JObject : IJValue
    {
        public Dictionary<string, IJValue> Object;

        public string Render()
        {
            var sb = new StringBuilder();
            sb.Append("{");

            bool first = true;
            foreach (var kvp in Object)
            {
                if (!first) sb.Append(", ");
                first = false;

                sb.Append("\"");
                sb.Append(kvp.Key);
                sb.Append("\": ");
                sb.Append(kvp.Value.Render());
            }

            sb.Append("}");
            return sb.ToString();
        }
    }

    /// <summary>
    ///      A JSON array.
    /// </summary>
    struct JArray : IJValue
    {
        public IJValue[] Array;

        public string Render()
        {
            var sb = new StringBuilder();
            sb.Append("[");

            bool first = true;
            foreach (var val in Array)
            {
                if (!first) sb.Append(", ");
                first = false;

                sb.Append(val.Render());
            }

            sb.Append("]");
            return sb.ToString();
        }
    }

    /// <summary>
    ///     Concept for things that can be converted to or from JSON.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the thing to be converted.
    /// </typeparam>
    concept CJson<A>
    {
        IJValue ToJValue(A a);
        A FromJValue(IJValue val, out string error);
    }

    instance CJsonIJValue : CJson<IJValue>
    {
        IJValue ToJValue(IJValue a) => a;
        IJValue FromJValue(IJValue val, out string error)
        {
            error = null;
            return val;
        }
    }

    instance CJsonBool : CJson<bool>
    {
        IJValue ToJValue(bool a) => new JBool { Bool = a };
        bool FromJValue(IJValue val, out string error)
        {
            var jbool = (val as JBool?);
            error = (jbool == null) ? "not a JSON boolean" : null;
            return (jbool?.Bool == null) ? (bool)jbool?.Bool : default(bool);
        }
    }

    instance CJsonString : CJson<string>
    {
        IJValue ToJValue(string a) => a == null ? (IJValue) new JNull() : new JString { String = a };
        string FromJValue(IJValue val, out string error)
        {
            var jstring = (val as JString?);
            error = (jstring == null) ? "not a JSON string" : null;
            return jstring?.String;
        }
    }

    instance CJsonInt : CJson<int>
    {
        IJValue ToJValue(int a) => new JNumber { Number = a };
        int FromJValue(IJValue val, out string error)
        {
            var jnum = (val as JNumber?);
            error = (jnum == null) ? "not a JSON number" : null;
            return (jnum?.Number == null) ? (int)jnum?.Number : default(int);
        }
    }

    instance CJsonDouble : CJson<double>
    {
        IJValue ToJValue(double a) => new JNumber { Number = a };
        double FromJValue(IJValue val, out string error)
        {
            var jnum = (val as JNumber?);
            error = (jnum == null) ? "not a JSON number" : null;
            return (jnum?.Number == null) ? (double)jnum?.Number : default(double);
        }
    }

    instance CJsonArray<A, implicit CJsonA> : CJson<A[]> where CJsonA : CJson<A>
    {
        IJValue ToJValue(A[] a)
        {
            if (a == null) return new JNull();

            var ary = new IJValue[a.Length];
            for (int i = 0; i < a.Length; i++)
            {
                ary[i] = ToJValue(a[i]);
            }

            return new JArray { Array = ary };
        }

        A[] FromJValue(IJValue val, out string error)
        {
            error = null;

            var ary = (val as JArray?)?.Array;
            if (ary == null)
            {
                error = "not a JSON array";
                return null;
            }

            var a = new A[ary.Length];
            for (int i = 0; i < ary.Length; i++)
            {
                a[i] = FromJValue(ary[i], out error);
                if (error != null) return null;
            }

            return a;
        }
    }

    instance CJsonDict<A, implicit CJsonA> : CJson<IDictionary<string, A>> where CJsonA : CJson<A>
    {
        IJValue ToJValue(IDictionary<string, A> adict)
        {
            if (adict == null) return new JNull();

            var obj = new Dictionary<string, IJValue>(adict.Count);
            foreach (var binding in adict)
            {
                obj[binding.Key] = ToJValue(binding.Value);
            }

            return new JObject { Object = obj };
        }

        IDictionary<string, A> FromJValue(IJValue val, out string error)
        {
            error = null;

            var obj = (val as JObject?)?.Object;
            if (obj == null)
            {
                error = "not a JSON object";
                return null;
            }

            var adict = new Dictionary<string, A>(obj.Count);
            foreach (var binding in obj)
            {
                adict[binding.Key] = FromJValue(binding.Value, out error);
                if (error != null) return null;
            }

            return adict;
        }
    }

    // Toy instance for tuples, to show how you might do general object structures.

    instance CJsonTup2<T1, T2, implicit CT1, implicit CT2> : CJson<((string, T1), (string, T2))>
        where CT1 : CJson<T1>
        where CT2 : CJson<T2>
    {
        IJValue ToJValue(((string, T1), (string, T2)) tup)
        {
            return new JObject { Object = new Dictionary<string, IJValue> {
                    { tup.Item1.Item1, ToJValue(tup.Item1.Item2) },
                    { tup.Item2.Item1, ToJValue(tup.Item2.Item2) }
                }
            };
        }

        ((string, T1), (string, T2)) FromJValue(IJValue val, out string error)
        {
            error = null;

            var obj = (val as JObject?)?.Object;
            if (obj == null)
            {
                error = "not a JSON object";
                return default(((string, T1), (string, T2)));
            }

            if (obj.Count != 2)
            {
                error = $"JSON object incorrect size: expected 2, got {obj.Count}";
                return default(((string, T1), (string, T2)));
            }

            bool got1 = false;
            string key1 = null;
            T1 val1 = default(T1);
            bool got2 = false;
            string key2 = null;
            T2 val2 = default(T2);
            string tmpError = null;

            foreach (var kvp in obj)
            {
                if (!got1)
                {
                    val1 = CT1.FromJValue(kvp.Value, out tmpError);
                    if (tmpError == null)
                    {
                        got1 = true;
                        key1 = kvp.Key;
                        continue;
                    }
                }
                if (!got2)
                {
                    val2 = CT2.FromJValue(kvp.Value, out tmpError);
                    if (tmpError == null)
                    {
                        got2 = true;
                        key2 = kvp.Key;
                        continue;
                    }
                }
                error = tmpError ?? "Invalid item in object";
                return default(((string, T1), (string, T2)));
            }

            return ((key1, val1), (key2, val2));
        }
    }

    class Program
    {
        static IJValue Jsonify<A, implicit JA>(A a) where JA : CJson<A> => ToJValue(a);

        static void Main(string[] args)
        {
            var obj = new Dictionary<string, string>
            {
                {"title", "Simon Peyton Jones: papers"},
                {"snippet", "Tackling the awkward squad: monadic input/output, concurrency, exceptions, and foreign-language calls in Haskell"},
                {"url", "http://research.microsoft.com/~simonpj/papers/marktoberdorf/" }
            };
            // TODO: type inference should be able to do this
            var json = Jsonify((IDictionary<string, string>)obj);

            Console.Out.WriteLine(json.Render());

            var obj2 = (("name", "Nineteen Eighty-Four"), ("year", 1948));
            var json2 = Jsonify(obj2);

            Console.Out.WriteLine(json2.Render());
        }
    }
}
