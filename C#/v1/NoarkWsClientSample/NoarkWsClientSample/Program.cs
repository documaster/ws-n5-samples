using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLine;
using Documaster.WebApi.Client.Noark5;
using Documaster.WebApi.Client.Noark5.Client;
using Documaster.WebApi.Client.Noark5.NoarkEntities;
using Documaster.WebApi.Client.IDP;
using Documaster.WebApi.Client.IDP.Oauth2;

namespace NoarkWsClientSample
{
    static class Program
    {
        public static void Main(string[] args)
        {
            var options = ParserCommandLineArguments(args);

            DocumasterClients documasterClients = new DocumasterClients(options);

            SystemInitializationSample initializationSample = new SystemInitializationSample(documasterClients);
            initializationSample.Execute();

            IntegrationSample integrationSample =
                new IntegrationSample(documasterClients, options.TestFile1, options.TestFile2);
            integrationSample.Execute();

            SignOffSample signOffSample = new SignOffSample(documasterClients);
            signOffSample.SignOffRegistryEntry("56", new List<string>() { "73" });

            FinalizationSample finalizationSample = new FinalizationSample(documasterClients);
            finalizationSample.FinalizeObjectsInJournal("34", "35", "36", "37");
            finalizationSample.FinalizeObjectsInArchive("42", "43", "44", "45");

            QuerySample querySample = new QuerySample(documasterClients);
            querySample.GetCodeLists();
            querySample.GetCaseFilesByExternalId("14", "2344-11", "External system");
            querySample.GetCaseFileBySecondaryClass("14", "45503", "John Doe");
            querySample.GetRegistryEntriesCreatedInDateRange("14", DateTime.Now.AddDays(-2), DateTime.Now);

            FullTextSearchSample fullTextSearchSample = new FullTextSearchSample(documasterClients);
            fullTextSearchSample.Search();
        }

        private static Options ParserCommandLineArguments(string[] args)
        {
            Options opts = null;

            var parseResult = Parser.Default
                .ParseArguments<Options>(args)
                .WithParsed(options => opts = options);

            if (parseResult.Tag == ParserResultType.NotParsed)
            {
                throw new Exception("Failed to parse command line arguments!");
            }

            return opts;
        }
    }
}
