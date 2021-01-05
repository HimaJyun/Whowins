using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using whowins.whois;

namespace whowins {
    class Program {
        static void Main(string[] args) {
            Option option;
            try {
                option = new Option(new Queue<string>(args));
            } catch (FormatException e) {
                Console.Error.WriteLine(e.Message);
                ShowHelp(Console.Error);
                return;
            }
            // ファイル作成があるなら作って終わる。
            if (option.Create != null) {
                Config.CreateConfig(option.Create);
                Console.Error.WriteLine($"Created config: {option.Create}");
                return;
            }

            // クエリが指定されていない
            if (option.Query == null) {
                ShowHelp(Console.Error);
                return;
            } else if (option.Query.IndexOf("://") != -1) {
                // 指定されてるけどhttp://が入ってるっぽいぞ
                option.Query = new Uri(option.Query).Host;
            }

            Config config;
            if (option.File == null) {
                // 未設定ならデフォルトで。
                config = Config.Load();
            } else {
                // 設定が指定されてたらそれをロードする
                config = Config.Load(option.File);
            }

            Whois whois;
            if (option.Verbose) {
                // 饒舌
                whois = new Whois(config, Console.Error);
            } else {
                // 寡黙
                whois = new Whois(config);
            }

            Console.WriteLine(whois.Lookup(option.Query, option.Host));
        }

        private static void ShowHelp(TextWriter writer) {
            void w(string s) => writer.WriteLine(s);
            var asm = Assembly.GetExecutingAssembly().GetName();

            w($"{asm.Name.ToString()} v{asm.Version.ToString()} by @HimaJyun( https://jyn.jp/ )");
            w("");
            w("Usage: whowins [option] <query>");
            w("");
            w("Option:");
            w("-h/--host         Specify host    (Ex: -h host, -h host:port)");
            w("-f/--file         Specify config  (Ex: -f ./whowins.conf)");
            w("-c/--create       Create config   (Ex: -c ./whowins.conf)");
            w("-v/--verbose      Verbose mode");
            w("--help/--version  Show help and version");
            w("");
        }

        private class Option {
            public bool Verbose { get; private set; } = false;
            public string Create { get; private set; } = null;
            public string File { get; private set; } = null;
            public string Host { get; private set; } = null;
            public string Query { get; set; } = null;
            public Option(Queue<string> args) {
                if (args.Count == 0) {
                    throw new FormatException("");
                }
                void setQuery(string s) => Query = s;
                Action<string> setter = setQuery;

                while (args.Count != 0) {
                    var arg = args.Dequeue();
                    switch (arg) {
                        case "--help":
                        case "--version":
                            throw new FormatException("");
                        case "-v":
                        case "--verbose":
                            Verbose = true;
                            continue;
                        case "-f":
                        case "--file":
                            setter = (s => { File = s; setter = setQuery; });
                            continue;
                        case "-h":
                        case "--host":
                            setter = (s => { Host = s; setter = setQuery; });
                            continue;
                        case "-c":
                        case "--create":
                            setter = (s => { Create = s; setter = setQuery; });
                            continue;
                        default:
                            setter(arg);
                            continue;
                    }
                }
            }
        }
    }
}
