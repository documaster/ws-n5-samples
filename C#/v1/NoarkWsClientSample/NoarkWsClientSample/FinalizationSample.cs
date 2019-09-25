using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Documaster.WebApi.Client.Noark5;
using Documaster.WebApi.Client.Noark5.Client;
using Documaster.WebApi.Client.Noark5.NoarkEntities;

namespace NoarkWsClientSample
{
    public class FinalizationSample
    {
        private readonly DocumasterClients documasterClients;

        public FinalizationSample(DocumasterClients documasterClients)
        {
            this.documasterClients = documasterClients;
        }

        public void FinalizeObjectsInJournal(string seriesId,
            string caseFileId,
            string registryEntryId,
            string documentDescriptionId
        )
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            // Note that currently finalizing objects will not finalize child objects!
            // We first finalize the objects in the bottom of the hierarchy, and then finalize parent objects up to Series.

            // Finalize series by setting changing the series status to Closed Period (P).
            Arkivdel series = GetNoarkEntityById<Arkivdel>(seriesId);
            series.Arkivdelstatus = Arkivdelstatus.AVSLUTTET_PERIODE;

            // Finalize case file by changing the case file status to Finalized (A).
            Saksmappe caseFile = GetNoarkEntityById<Saksmappe>(caseFileId);
            caseFile.Saksstatus = Saksstatus.AVSLUTTET;

            // Finalized registry entry by changing the record status to Archived (A).
            Journalpost registryEntry = GetNoarkEntityById<Journalpost>(registryEntryId);
            registryEntry.Journalstatus = Journalstatus.ARKIVERT;

            // Finalize document description by changing the document status to Finalized (F).
            Dokument documentDescription = GetNoarkEntityById<Dokument>(documentDescriptionId);
            documentDescription.Dokumentstatus = Dokumentstatus.DOKUMENTET_ER_FERDIGSTILT;

            client.Transaction()
                .Save(documentDescription)
                .Save(registryEntry)
                .Save(caseFile)
                .Save(series)
                .Commit();
        }

        public void FinalizeObjectsInArchive(string seriesId,
            string folderId,
            string basicRecordId,
            string documentDescriptionId
            )
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            // Note that currently finalizing objects will not finalize child objects!
            // We first finalize the objects in the bottom of the hierarchy, and then finalize parent objects up to Series. 

            // Finalize series by setting changing the series status to Closed Period (P).
            Arkivdel series = GetNoarkEntityById<Arkivdel>(seriesId);
            series.Arkivdelstatus = Arkivdelstatus.AVSLUTTET_PERIODE;

            // Finalize folder by setting the finalized date field.
            Mappe folder = GetNoarkEntityById<Mappe>(folderId);
            folder.AvsluttetDato = DateTime.Now;

            // Finalize basic record by setting the finalized date field.
            Basisregistrering basicRecord = GetNoarkEntityById<Basisregistrering>(basicRecordId);
            basicRecord.AvsluttetDato = DateTime.Now;

            // Finalize document description by changing the document status to Finalized (F).
            Dokument documentDescription = GetNoarkEntityById<Dokument>(documentDescriptionId);
            documentDescription.Dokumentstatus = Dokumentstatus.DOKUMENTET_ER_FERDIGSTILT;

            client.Transaction()
                .Save(documentDescription)
                .Save(basicRecord)
                .Save(folder)
                .Save(series)
                .Commit();
        }

        private T GetNoarkEntityById<T>(string id) where T : INoarkEntity
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            QueryResponse<T> queryResponse = client.Query<T>("id=@id", 1)
                .AddQueryParam("@id", id)
                .Execute();

            if (queryResponse.Results.Any())
            {
                return queryResponse.Results.First();
            }

            throw new Exception($"Object of type '{typeof(T).Name}' with id '{id}' was not found!");
        }
    }
}
