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
    public class SignOffSample
    {
        private readonly DocumasterClients documasterClients;

        public SignOffSample(DocumasterClients documasterClients)
        {
            this.documasterClients = documasterClients;
        }

        public void SignOffRegistryEntry(string registryEntryId, List<string> associatedRegistryEntryIds = null)
        {
            NoarkClient client = this.documasterClients.GetNoarkClient();

            Avskrivning signOff = new Avskrivning(Avskrivningsmaate.TATT_TIL_ETTERRETNING);

            Transaction transaction = client.Transaction()
                .Save(signOff)
                .Link(signOff.LinkJournalpost(registryEntryId));

            if (associatedRegistryEntryIds != null)
            {
                foreach (string associatedRegistryEntryId in associatedRegistryEntryIds)
                {
                    transaction.Link(signOff.LinkTilknyttetJournalpost(associatedRegistryEntryId));
                }
            }

            transaction.Commit();
        }
    }
}
