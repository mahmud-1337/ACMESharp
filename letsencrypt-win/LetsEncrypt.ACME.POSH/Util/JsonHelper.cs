﻿using LetsEncrypt.ACME.HTTP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LetsEncrypt.ACME.POSH.Util
{
    public static class JsonHelper
    {
        private static Newtonsoft.Json.JsonSerializerSettings JSS =
                new Newtonsoft.Json.JsonSerializerSettings
                {
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto,
                    Converters = new List<JsonConverter>
                    {
                        AcmeEntitySerializer.INSTANCE
                    }
                };


        public static void Save(Stream s, object obj)
        {
            using (var w = new StreamWriter(s))
            {
                w.Write(JsonConvert.SerializeObject(obj, JSS));
            }
        }

        public static T Load<T>(Stream s)
        {
            using (var r = new StreamReader(s))
            {
                return JsonConvert.DeserializeObject<T>(r.ReadToEnd(), JSS);
            }
        }

        public class AcmeEntitySerializer : Newtonsoft.Json.JsonConverter
        {
            public static readonly AcmeEntitySerializer INSTANCE = new AcmeEntitySerializer();

            public override bool CanConvert(Type objectType)
            {
                return typeof(AcmeServerDirectory) == objectType
                        || typeof(LinkCollection) == objectType;
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (typeof(AcmeServerDirectory) == objectType)
                {
                    var jarr = JArray.Load(reader);
                    var sdir = existingValue as AcmeServerDirectory;

                    if (jarr == null)
                        sdir = null;
                    else
                    {
                        if (sdir == null)
                            sdir = new AcmeServerDirectory();
                        foreach (var jt in jarr)
                        {
                            var kv = jt.ToObject<KeyValuePair<string, string>>();
                            sdir[kv.Key] = kv.Value;
                        }
                    }

                    return sdir;
                }
                else if (typeof(LinkCollection) == objectType)
                {
                    var jarr = JArray.Load(reader);
                    var lc = existingValue as LinkCollection;

                    if (jarr == null)
                        lc = null;
                    else
                    {
                        if (lc == null)
                            lc = new LinkCollection();
                        foreach (var jt in jarr)
                        {
                            lc.Add(new Link(jt.ToObject<string>()));
                        }
                    }
                }



                throw new NotSupportedException("Unsupported type");
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var objectType = value.GetType();
                if (typeof(AcmeServerDirectory) == objectType)
                {
                    var sd = (AcmeServerDirectory)value;
                    var jt = JToken.FromObject(sd);
                    jt.WriteTo(writer);
                }
                else if (typeof(LinkCollection) == objectType)
                {
                    var lc = (LinkCollection)value;
                    writer.WriteStartArray();
                    foreach (var l in ((IEnumerable<Link>)lc))
                        writer.WriteValue(l.Value);
                    writer.WriteEndArray();
                }
                else
                {
                    throw new NotSupportedException("Unsupported type");
                }
            }
        }
    }
}
