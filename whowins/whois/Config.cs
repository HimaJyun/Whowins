using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace whowins.whois {

    public class Config {
        private enum Mode { Global, Domain }

        private Dictionary<string, string> whois = new Dictionary<string, string>();

        private Config() { }
        public static Config Load() {
            using (var reader = new StringReader(Properties.Resources.Config)) {
                return Load(reader);
            }
        }
        public static Config Load(string path) {
            using (var reader = new StreamReader(path)) {
                return Load(reader);
            }
        }
        public static Config Load(TextReader reader) {
            var result = new Config();
            var mode = Mode.Global;

            string line;
            while ((line = reader.ReadLine()) != null) {
                if (line.Length == 0) {
                    continue;
                } else if (line == "[Domain]") {
                    mode = Mode.Domain;
                    continue;
                } else if (mode == Mode.Global) {
                    continue;
                }

                // コメントを取り除く
                line = line.Split('#')[0];
                line = line.Trim();
                if (line.Length == 0) {
                    continue;
                }

                var ary = line.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);

                if (mode == Mode.Domain) {
                    result.whois.Add(ary[0], ary[1]);
                }
            }
            return result;
        }

        public string GetWhoisServer(string domain) {
            var builder = new StringBuilder(domain);
            string tld;
            while (true) {
                tld = builder.ToString();
                if (whois.ContainsKey(tld)) {
                    return whois[tld];
                }

                // example.co.jp -> co.jp
                var i = tld.IndexOf('.');
                if (i == -1) {
                    break;
                }
                builder.Remove(0, 1 + i);

            }

            return tld + ".whois-servers.net";
        }

        public static void CreateConfig(string path) {
            using (var writer = new StreamWriter(new FileStream(path, FileMode.Create, FileAccess.Write))) {
                writer.Write(Properties.Resources.Config);
            }
        }

    }
}
