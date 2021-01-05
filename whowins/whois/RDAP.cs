/*using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace whowins.whois {
    public class RDAP {
        public readonly string Raw;

        public readonly string Handle;
        public readonly string StartAddress;
        public readonly string EndAddress;
        public readonly string Country;
        public readonly string Name;
        public readonly Dictionary<string, string[]> Remarks;

        public RDAP(string json) {
            this.Raw = json;
            var obj = JObject.Parse(Raw);

            Handle = (string)obj["handle"];
            StartAddress = (string)obj["startAddress"];
            EndAddress = (string)obj["endAddress"];
            Country = (string)obj["country"];
            Name = (string)obj["name"];

            Remarks = obj["remarks"].ToDictionary(
                o => (string)o["title"],
                o => o["description"].Values().Select(o2 => (string)o2).ToArray()
            );
        }

        public override string ToString() {
            var result = new StringBuilder();
            void a(string s) => result.Append(s);
            void b(string s) => result.AppendLine(s);
            a("handle:        "); b(Handle);
            a("startAddress:  "); b(StartAddress);
            a("endAddress:    "); b(EndAddress);
            a("country:       "); b(Country);
            a("name:          "); b(Name);
            b("");
            foreach (var remark in Remarks) {
                foreach (var v in remark.Value) {
                    a(remark.Key); a(":  "); b(v);
                }
            }

            return result.ToString();
        }
    }
}*/