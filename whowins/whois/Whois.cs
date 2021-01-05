using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace whowins.whois {
    public class Whois {
        private const int WHOIS_PORT = 43;
        private static readonly string[] RDAP_SERVERS = {
            "https://rdap.apnic.net/",
            "https://rdap.db.ripe.net/",
            "https://rdap.afrinic.net/rdap/",
        };

        public enum LookupType { IPv4, IPv6, Domain }
        private static readonly Random rnd = new Random();

        private ThreadLocal<IdnMapping> idn = new ThreadLocal<IdnMapping>(() => new IdnMapping());

        private readonly TextWriter writer;
        private readonly Config config;
        /// <summary>
        /// 出力のフォーマットを行うか？
        /// </summary>
        public bool Format = true;

        public Whois(Config config) : this(config, TextWriter.Null) { }
        public Whois(Config config, TextWriter writer) {
            this.writer = writer;
            this.config = config;
        }

        /// <summary>
        /// 問い合わせタイプを確認して自動で問い合わせを行います。
        /// </summary>
        /// <param name="query">問い合わせするクエリ</param>
        /// <param name="host">問い合わせ先サーバー</param>
        /// <returns>問い合わせ結果</returns>
        public string Lookup(string query, string host = null) {
            LookupType type = DetectLookupType(query);

            switch (type) {
                case LookupType.Domain:
                    return LookupDomain(query, host);
                case LookupType.IPv4:
                case LookupType.IPv6:
                    return LookupIP(query, host);
            }
            return null; // 来るわけない
        }

        /// <summary>
        /// ドメインのWhois情報を取得します。
        /// </summary>
        /// <param name="query">取得するドメイン</param>
        /// <param name="server">Whoisの取得先サーバー</param>
        /// <returns>取得結果</returns>
        public string LookupDomain(string query, string server = null) {
            var builder = new StringBuilder();
            string recursion = null;

            query = PunyEncode(query);

            if (server == null) {
                server = config.GetWhoisServer(query);
            }
            server = PunyEncode(server);

            int port = SplitPort(server, out var host);
            if (port == -1) {
                port = WHOIS_PORT;
            }

            writer.WriteLine($"Connecting to \"{host}\" Port {port}...");
            writer.WriteLine($"Query \"{query}\"");
            using (var tcp = new TcpClient()) {
                try {
                    tcp.Connect(host, port);
                } catch (SocketException e) {
                    switch (e.SocketErrorCode) {
                        case SocketError.HostNotFound:
                            return $"Host not found: {host}";
                        default:
                            throw;
                    }
                }
                using (NetworkStream network = tcp.GetStream())
                using (var reader = new StreamReader(network)) {
                    // Request書き込み
                    var data = Encoding.ASCII.GetBytes((query + "\r\n").ToCharArray());
                    network.Write(data, 0, data.Length);

                    // めっちゃ受信する
                    string line;
                    while ((line = reader.ReadLine()) != null) {
                        builder.AppendLine(line);
                        // 再帰問い合わせが必要か確認
                        line = line.Trim();
                        if (line.ToLower().IndexOf("registrar whois server:") == 0) { // ==0で先頭一致に限定させる
                            line = line.Substring("registrar whois server:".Length).Trim();
                            if (line.Length != 0) {
                                recursion = line;
                            }
                        }
                    }
                }
            }

            if (recursion != null && recursion != host) {
                writer.WriteLine($"Recursive Query: {recursion}");
                builder.Append(LookupDomain(query, recursion));
            }

            return builder.ToString();
        }

        /// <summary>
        /// IPアドレスの所有者を確認します。実験的機能のため取得した情報がそのまま出力されます。
        /// </summary>
        /// <param name="query">確認したいIPアドレス</param>
        /// <param name="host">問い合わせを行うサーバー</param>
        /// <returns>取得した情報、恐らくjson</returns>
        public string LookupIP(string query, string host = null) {
            writer.WriteLine("This function is \"Experimental\".");

            if (host == null) {
                var i = rnd.Next(RDAP_SERVERS.Length);
                host = RDAP_SERVERS[i];
            }

            var rdap = $"{host}ip/{query}";
            using (var web = new WebClient()) {
                writer.WriteLine($"Connecting to \"{rdap}\"...");
                return web.DownloadString(rdap);
            }
        }

        private int SplitPort(string server, out string host) {
            int count(string s, char c) => s.Length - s.Replace(c.ToString(), "").Length;
            host = server;

            if (count(server, ':') >= 2) { // :が2個以上あるならクソIPv6
                if (server[0] == '[') {
                    // [で始まってる
                    var ary = server.Split(']');
                    host = ary[0].Substring(1); // "["を飛ばした分
                    if (ary.Length > 1) {
                        // Last() == ":"で分けた後ろ == ポート
                        return int.Parse(ary[1].Split(':').Last());
                    }
                } else {
                    // 直接書かれてる=ポート指定はない
                    host = server;
                }
            } else {
                var ary = server.Split(':');
                host = ary[0];
                if (ary.Length > 1) {
                    return int.Parse(ary[1]);
                }
            }
            return -1;
        }


        public LookupType DetectLookupType(string query) {
            if (query.IndexOf(':') > 0) {
                // ":"を含むならIPv6
                return LookupType.IPv6;
            } else {
                foreach (char c in query) {
                    // 何らかの文字があるならドメインとする
                    if ((c < '0' || '9' < c) && c != '.') {
                        return LookupType.Domain;
                    }
                }
                // そうでもなさそうならIPv4
                return LookupType.IPv4;
                // "...."とかだとv4判定になるけど、そんな値を入れるバカの事なんか考慮しない
            }
        }

        public string PunyEncode(string domain) {
            return idn.Value.GetAscii(domain);
        }
    }
}
