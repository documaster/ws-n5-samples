using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoarkWsClientSample
{
    public class Options
    {
        [Option("idpaddr", Required = true, HelpText = "Idp server address, such as  https://clientname.dev.documaster.tech/idp/oauth2")]
        public string IdpServerAddress { get; set; }

        [Option("clientid", Required = true, HelpText = "Idp Client Id")]
        public string ClientId { get; set; }

        [Option("clientsecret", Required = true, HelpText = "Idp Client Secret")]
        public string ClientSecret { get; set; }

        [Option("username", Required = true, HelpText = "Username")]
        public string Username { get; set; }

        [Option("password", Required = true, HelpText = "Password")]
        public string Password { get; set; }

        [Option("addr", Required = true, HelpText = "Server address, such as  https://clientname.dev.documaster.tech:8083")]
        public string ServerAddress { get; set; }

        [Option("testfile1", Required = true, HelpText = "Path to a test file")]
        public string TestFile1 { get; set; }

        [Option("testfile2", Required = true, HelpText = "Path to a test file")]
        public string TestFile2 { get; set; }
    }
}
