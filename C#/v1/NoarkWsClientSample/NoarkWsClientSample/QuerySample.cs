using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Documaster.WebApi.Client.Noark5.Client;
using Documaster.WebApi.Client.Noark5.NoarkEntities;

namespace NoarkWsClientSample
{
    public class QuerySample
    {
        private readonly DocumasterClients documasterClients;

        public QuerySample(DocumasterClients documasterClients)
        {
            this.documasterClients = documasterClients;
        }

        public void GetCodeLists()
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            Console.WriteLine("Get all available code lists");
            List<CodeList> allLists = client.CodeLists();

            foreach (CodeList codeList in allLists)
            {
                Console.WriteLine($"Field: {codeList.Field}. Type: {codeList.Type}. Values:");
                foreach (CodeValue codeValue in codeList.Values)
                {
                    Console.WriteLine($"===== Code: {codeValue.Code}. Value: {codeValue.Name}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Get a code list by field and/or type");
            CodeList documentTypeList = client.CodeLists("Dokument", "dokumenttype").First();

            foreach (CodeValue codeValue in documentTypeList.Values)
            {
                Console.WriteLine($"===== Code: {codeValue.Code}. Value: {codeValue.Name}");
            }
        }

        public void GetCaseFilesByExternalId(string seriesId, string externalId, string externalSystem)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            //Note that folders, basic records, registry entries and document descriptions might also have an external id attached.

            List<Saksmappe> caseFiles = new List<Saksmappe>();

            bool hasMoreResults = true;
            int pageSize = 30;
            int offset = 0;

            while (hasMoreResults)
            {
                QueryResponse<Saksmappe> queryResponse =
                    client.Query<Saksmappe>(
                            "refArkivdel.id=@seriesId && refEksternId.eksternID=@externalId && refEksternId.eksterntSystem=@externalSystem",
                            1)
                        .AddQueryParam("@seriesId", seriesId)
                        .AddQueryParam("@externalId", externalId)
                        .AddQueryParam("@externalSystem", externalSystem)
                        .SetOffset(offset)
                        .Execute();

                hasMoreResults = queryResponse.HasMore;
                offset += pageSize;
                caseFiles.AddRange(queryResponse.Results);
            }

            foreach (Saksmappe caseFile in caseFiles)
            {
                Console.WriteLine($"Case file: Id: {caseFile.Id}. Title: {caseFile.Tittel}");
            }
        }

        public void GetCaseFileBySecondaryClass(string seriesId, string secondaryClassId, string secondaryClassTitle)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            //Note that folders, basic records and registry entries might also be linked to a secondary class.

            List<Saksmappe> caseFiles = new List<Saksmappe>();

            bool hasMoreResults = true;
            int pageSize = 30;
            int offset = 0;

            while (hasMoreResults)
            {
                QueryResponse<Saksmappe> queryResponse = client
                    .Query<Saksmappe>(
                        "refArkivdel.id=@seriesId && refSekundaerKlasse.klasseIdent=@classId && refSekundaerKlasse.tittel=@title",
                        1)
                    .AddQueryParam("@seriesId", seriesId)
                    .AddQueryParam("@classId", secondaryClassId)
                    .AddQueryParam("@title", secondaryClassTitle)
                    .SetOffset(0)
                    .Execute();

                hasMoreResults = queryResponse.HasMore;
                offset += pageSize;
                caseFiles.AddRange(queryResponse.Results);
            }

            foreach (Saksmappe caseFile in caseFiles)
            {
                Console.WriteLine($"Case file: Id: {caseFile.Id}. Title: {caseFile.Tittel}");
            }
        }

        public void GetRegistryEntriesCreatedInDateRange(string seriesId, DateTime fromDate, DateTime toDate)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            List<Journalpost> registryEntries = new List<Journalpost>();

            bool hasMoreResults = true;
            int pageSize = 30;
            int offset = 0;

            while (hasMoreResults)
            {
                QueryResponse<Journalpost> queryResponse =
                    client.Query<Journalpost>("refMappe.refArkivdel.id=@seriesId && opprettetDato=[@from:@to]",
                            pageSize)
                        .AddQueryParam("@seriesId", seriesId)
                        .AddQueryParam("@from", fromDate)
                        .AddQueryParam("@to", toDate)
                        .SetOffset(offset)
                        .Execute();

                offset += pageSize;
                hasMoreResults = queryResponse.HasMore;
                registryEntries.AddRange(queryResponse.Results);
            }

            foreach (Journalpost registryEntry in registryEntries)
            {
                Console.WriteLine($"Registry entry: Id: {registryEntry.Id}. Title: {registryEntry.Tittel}");
            }
        }

        public void GetCaseFileByTwoSecondaryClassesUsingJoins(string secondaryClassIdent1, string secondaryClassIdent2)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            int pageSize = 1;

            QueryResponse<Saksmappe> queryResponse =
                 client.Query<Saksmappe>("#klasse1.klasseIdent=@klasseIdent1 && #klasse2.klasseIdent=@klasseIdent2",
                        pageSize)
                    .AddJoin("#klasse1", "refSekundaerKlasse")
                    .AddJoin("#klasse2", "refSekundaerKlasse")
                    .AddQueryParam("@klasseIdent1", secondaryClassIdent1)
                    .AddQueryParam("@klasseIdent2", secondaryClassIdent2)
                    .Execute();

            if (queryResponse.Results.Any())
            {
                Saksmappe saksmappe = queryResponse.Results.First();
                Console.WriteLine($"Found a case file with title '{saksmappe.Tittel}' linked to two secondary classes with klasseIdent '{secondaryClassIdent1}' and klasseIdent '{secondaryClassIdent2}'");
            }

        }
    }
}
