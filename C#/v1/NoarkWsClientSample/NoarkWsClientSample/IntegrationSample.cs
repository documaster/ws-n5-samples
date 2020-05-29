using Documaster.WebApi.Client.Noark5.Client;
using Documaster.WebApi.Client.Noark5.NoarkEntities;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Documaster.WebApi.Client.Noark5;

namespace NoarkWsClientSample
{
    public class IntegrationSample
    {
        private const string EXTERNAL_SYSTEM = "External System";

        private readonly DocumasterClients documasterClients;
        private readonly string testFile1;
        private readonly string testFile2;

        public IntegrationSample(DocumasterClients documasterClients, string testFile1, string testFile2)
        {
            this.documasterClients = documasterClients;
            this.testFile1 = testFile1;
            this.testFile2 = testFile2;
        }

        public void Execute()
        {
            #region Set sample values

            string seriesTitle = "Skole";
            string primaryClassTitle = "Tilbud";
            string secondaryClassTitle = "John Smith";
            string secondaryClassId = "45-67771344-7";
            string administrativeUnit = "Privat GSK";
            string screeningCode = "UOFF";
            string documentType = "Rapport";
            string caseFileTitle = "John Smith - Test Skole";
            string caseFileExternalId = "4551-54555";
            string caseResponsibleName = "Maria Doe";
            string caseResponsibleId = "3445555";
            string registryEntryTitle = "Half-year report - 2019 - John Smith";
            string registryEntryExternalId = "45-67771344-7";

            #endregion

            // We assume that a series and a primary class already exist and we will just fetch them.
            // SystemInitializationSample.cs shows how to create these objects.
            Arkivdel series = GetSeriesByTitle(seriesTitle);

            if (series == null)
            {
                Console.WriteLine($"Did not find a series with title {seriesTitle}!");
                return;
            }

            Klasse primaryClass = GetClassByTitle(series.RefPrimaerKlassifikasjonssystem, primaryClassTitle);

            if (primaryClass == null)
            {
                Console.WriteLine($"Could not find a class with title {primaryClassTitle}!");
                return;
            }

            /* Now, we are going to fetch or create a secondary class.
             * A secondary class id and title might be a person's number and name, such as 12356 John Doe.
             * To fetch the secondary class we need to know in which secondary classification system the class resides.
             * A series can be linked to more than one secondary classification systems. 
             * For simplicity, in this example we will randomly pick one of the series's secondary classification systems.
             */

            Klassifikasjonssystem secondaryClassificationSystem = GetSecondaryClassificationSystem(series.Id);

            if (secondaryClassificationSystem == null)
            {
                Console.WriteLine("Series does not have a secondary classification system!");
                return;
            }

            Klasse secondaryClass = GetClassByTitle(secondaryClassificationSystem.Id, secondaryClassTitle)
                                    ??
                                    CreateClass(secondaryClassificationSystem.Id, secondaryClassId,
                                        secondaryClassTitle);


            // We also need to find the code values used for the creation of an Archive structure.
            // SystemInitializationSample.cs shows how to create these code values.
            CodeValue administrativeUnitCodeValue = GetAdministrativeUnitByName(administrativeUnit);
            CodeValue screeningCodeValue = GetScreeningCodeByName(screeningCode);
            CodeValue documentTypeCodeValue = GetDocumentTypeByName(documentType);

            if (administrativeUnitCodeValue == null)
            {
                Console.WriteLine($"Could not find an administrative unit code value '{administrativeUnit}'!");
                return;
            }

            if (screeningCodeValue == null)
            {
                Console.WriteLine($"Could not find an screening code value '{seriesTitle}'!");
                return;
            }

            if (documentTypeCodeValue == null)
            {
                Console.WriteLine($"Could not find an document type code value '{documentType}'!");
                return;
            }

            SubmitData(series,
                primaryClass,
                secondaryClass,
                new AdministrativEnhet(administrativeUnitCodeValue.Code),
                new Skjerming(screeningCodeValue.Code),
                new Dokumenttype(documentTypeCodeValue.Code),
                caseFileTitle,
                caseFileExternalId,
                caseResponsibleName,
                caseResponsibleId,
                registryEntryTitle,
                registryEntryExternalId);
        }

        private void SubmitData(
            Arkivdel series,
            Klasse primaryClass,
            Klasse secondaryClass,
            AdministrativEnhet administrativeUnit,
            Skjerming screeningCode,
            Dokumenttype documentType,
            string caseFileTitle,
            string caseFileExternalId,
            string caseResponsibleName,
            string caseResponsibleId,
            string registryEntryTitle,
            string registryEntryExternalId)
        {
            #region Case file

            Saksmappe caseFile = new Saksmappe(caseFileTitle, administrativeUnit)
            {
                Saksansvarlig = caseResponsibleName,
                SaksansvarligBrukerIdent = caseResponsibleId
            };

            EksternId caseFileExternalIdObj = new EksternId(EXTERNAL_SYSTEM, caseFileExternalId);

            #endregion Case file

            #region Registry entry

            Journalpost registryEntry = new Journalpost(registryEntryTitle, Journalposttype.UTGAAENDE_DOKUMENT)
            {
                Skjerming = screeningCode
            };

            registryEntry.VirksomhetsspesifikkeMetadata.AddBsmFieldValues("gr-1", "f-string", "value 1");

            EksternId registryEntryExternalIdObj = new EksternId(EXTERNAL_SYSTEM, registryEntryExternalId);

            Korrespondansepart correspondenceParty =
                new Korrespondansepart(Korrespondanseparttype.AVSENDER, "John Smith");

            #endregion Registry entry

            #region Documents

            //Upload two files

            Dokumentfil mainFile = UploadDocument(this.testFile1);
            Dokumentfil attachmentFile = UploadDocument(this.testFile2);

            //Link the first document description to the registry entry as main document (HOVEDDOKUMENT).
            //Subsequent document descriptions will be linked as attachments (VEDLEGG).

            Dokument mainDocumentDescription =
                new Dokument("Main Document", TilknyttetRegistreringSom.HOVEDDOKUMENT)
                {
                    Dokumenttype = documentType,
                };

            Dokumentversjon mainDocumentVersion =
                new Dokumentversjon(Variantformat.ARKIVFORMAT, ".pdf", mainFile);

            Dokument attachmentDocumentDescription =
                new Dokument("Attachment", TilknyttetRegistreringSom.VEDLEGG)
                {
                    Dokumenttype = documentType //here might as well be used another type
                };

            Dokumentversjon attachmentDocumentVersion =
                new Dokumentversjon(Variantformat.ARKIVFORMAT, ".pdf", attachmentFile);

            #endregion Documents

            NoarkClient client = this.documasterClients.GetNoarkClient();

            TransactionResponse transactionResponse = client.Transaction()
                .Save(caseFile)
                .Link(caseFile.LinkArkivdel(series))
                .Link(caseFile.LinkPrimaerKlasse(primaryClass))
                .Link(caseFile.LinkSekundaerKlasse(secondaryClass))
                .Save(caseFileExternalIdObj)
                .Link(caseFileExternalIdObj.LinkMappe(caseFile))
                .Save(registryEntry)
                .Link(registryEntry.LinkMappe(caseFile))
                .Save(correspondenceParty)
                .Link(correspondenceParty.LinkRegistrering(registryEntry))
                .Save(registryEntryExternalIdObj)
                .Link(registryEntryExternalIdObj.LinkRegistrering(registryEntry))
                .Save(mainDocumentDescription)
                .Link(mainDocumentDescription.LinkRegistrering(registryEntry))
                .Save(mainDocumentVersion)
                .Link(mainDocumentVersion.LinkDokument(mainDocumentDescription))
                .Save(attachmentDocumentDescription)
                .Link(attachmentDocumentDescription.LinkRegistrering(registryEntry))
                .Save(attachmentDocumentVersion)
                .Link(attachmentDocumentVersion.LinkDokument(attachmentDocumentDescription))
                .Commit();

            // When new objects are initialized, a temporary Id is assigned to them.
            // transactionResponse.Saved contains a mapping between the temporary id's and the saved objects with their permanent id's
            Dictionary<string, INoarkEntity> savedObjects = transactionResponse.Saved;

            string template = "{0}: Temporary Id: {1} Permanent Id: {2}";

            Console.WriteLine(String.Format(template, "Case file", caseFile.Id, savedObjects[caseFile.Id].Id));
            Console.WriteLine(String.Format(template, "Registry entry", registryEntry.Id,
                savedObjects[registryEntry.Id].Id));
            Console.WriteLine(String.Format(template, "Main document description", mainDocumentDescription.Id,
                savedObjects[mainDocumentDescription.Id].Id));
            Console.WriteLine(String.Format(template, "Attachment document description",
                attachmentDocumentDescription.Id, savedObjects[attachmentDocumentDescription.Id].Id));
        }

        private Arkivdel GetSeriesByTitle(string title)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            QueryResponse<Arkivdel> queryResponse = client.Query<Arkivdel>("tittel=@title", 1)
                .AddQueryParam("@title", title)
                .Execute();

            IEnumerable<Arkivdel> results = queryResponse.Results;

            if (!results.Any())
            {
                return null;
            }

            if (queryResponse.HasMore)
            {
                Console.WriteLine($"Found more than two series with title {title}!");
            }

            return results.First();
        }

        private Klassifikasjonssystem GetSecondaryClassificationSystem(string seriesId)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            QueryResponse<Klassifikasjonssystem> queryResponse = client
                .Query<Klassifikasjonssystem>("refArkivdelSomSekundaer.id=@seriesId", 1)
                .AddQueryParam("@seriesId", seriesId)
                .Execute();

            if (!queryResponse.Results.Any())
            {
                return null;
            }

            return queryResponse.Results.First();
        }

        private Klasse GetClassByTitle(string classificationSystemId, string title)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            QueryResponse<Klasse> queryResponse = client
                .Query<Klasse>("refKlassifikasjonssystem.id=@ksId && tittel=@title", 1)
                .AddQueryParam("@ksId", classificationSystemId)
                .AddQueryParam("@title", title)
                .Execute();

            IEnumerable<Klasse> results = queryResponse.Results;

            if (!results.Any())
            {
                return null;
            }

            if (queryResponse.HasMore)
            {
                Console.WriteLine($"Found more than two classes with title {title}!");
            }

            return results.First();
        }

        private Klasse CreateClass(string classificationSystemId, string classId, string title)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            //When new objects are initialized, a temporary Id is assigned to them.
            Klasse klass = new Klasse(classId, title);

            TransactionResponse transactionResponse = client.Transaction()
                .Save(klass)
                .Link(klass.LinkKlassifikasjonssystem(classificationSystemId))
                .Commit();

            //transactionResponse.Saved contains a mapping between the temporary id's and the saved objects with their permament id's
            Klasse savedClass = transactionResponse.Saved[klass.Id] as Klasse;
            Console.WriteLine($"Created a new class '{savedClass.Tittel}'. Permanent Id: {savedClass.Id}.");

            return savedClass;
        }

        private CodeValue GetScreeningCodeByName(string name)
        {
            NoarkClient noarkClient = this.documasterClients.GetNoarkClient();

            CodeList screeningCodeList = noarkClient.CodeLists(null, "skjerming").First();

            return screeningCodeList.Values.Find(codeValue => codeValue.Name.Equals(name));
        }

        private CodeValue GetAdministrativeUnitByName(string name)
        {
            NoarkClient noarkClient = documasterClients.GetNoarkClient();

            CodeList administrativeUnitCodeList = noarkClient.CodeLists("Saksmappe", "administrativEnhet").First();

            return administrativeUnitCodeList.Values.Find(codeValue => codeValue.Name.Equals(name));
        }

        private CodeValue GetDocumentTypeByName(string name)
        {
            NoarkClient noarkClient = this.documasterClients.GetNoarkClient();

            CodeList documentTypeCodeList = noarkClient.CodeLists("Dokument", "dokumenttype").First();

            return documentTypeCodeList.Values.Find(codeValue => codeValue.Name.Equals(name));
        }

        private Dokumentfil UploadDocument(string filePath)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            return client.Upload(filePath);
        }
    }
}
