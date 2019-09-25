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
    public class SystemInitializationSample
    {
        private readonly DocumasterClients documasterClients;

        public SystemInitializationSample(DocumasterClients documasterClients)
        {
            this.documasterClients = documasterClients;
        }

        public void Execute()
        {
            string fondsId = CreateFonds();
            string seriesId = CreateSeriesWithClassificationSystem(fondsId);
            CreateListValues();
            CreateBusinessSpecificMetaDataGroupsAndFields();
        }

        public string CreateFonds()
        {
            Console.WriteLine($"Create Fonds");
          
            NoarkClient client = this.documasterClients.GetNoarkClient();

            //When new objects are initialized, a temporary Id is assigned to them.
            Arkivskaper fondsCreator = new Arkivskaper("B7-23-W5", "John Smith");
            Arkiv fonds = new Arkiv("Arkiv");

            // Execute transaction
            TransactionResponse transactionResponse = client.Transaction()
                .Save(fonds)
                .Save(fondsCreator)
                .Link(fonds.LinkArkivskaper(fondsCreator))
                .Commit();

            //transactionResponse.Saved contains a mapping between the temporary id's and the saved objects with their permanent id's
            Arkiv savedFonds = transactionResponse.Saved[fonds.Id] as Arkiv;
            Arkivskaper savedFondsCreator = transactionResponse.Saved[fondsCreator.Id] as Arkivskaper;

            Console.WriteLine(
                $"Fonds creator '{savedFondsCreator.ArkivskaperNavn}' created. Temporary Id: {fondsCreator.Id}. Permanent Id: {savedFondsCreator.Id}.");
            Console.WriteLine(
                $"Fonds '{savedFonds.Tittel}' created. Temporary Id: {fonds.Id}. Permanent Id: {savedFonds.Id}.");

            // Return the id of the new fonds
            return savedFonds.Id;
        }

        public string CreateSeriesWithClassificationSystem(string fondsId)
        {
            Console.WriteLine($"Create a Series with a classification system and one class.");
            NoarkClient client = this.documasterClients.GetNoarkClient();

            //When new objects are initialized, a temporary Id is assigned to them.
            Klassifikasjonssystem classificationSystem = new Klassifikasjonssystem("Oppkvest");
            Klasse klass = new Klasse("01", "Tilbud");
            Arkivdel series = new Arkivdel("Barnehage");

            TransactionResponse transactionResponse = client.Transaction()
                .Save(series)
                .Link(series.LinkArkiv(fondsId))
                .Save(classificationSystem)
                .Link(series.LinkPrimaerKlassifikasjonssystem(classificationSystem))
                .Save(klass)
                .Link(klass.LinkKlassifikasjonssystem(classificationSystem))
                .Commit();

            //transactionResponse.Saved contains a mapping between the temporary id's and the saved objects with their permanent id's
            Klassifikasjonssystem savedClassificationSystem =
                transactionResponse.Saved[classificationSystem.Id] as Klassifikasjonssystem;
            Klasse savedClass = transactionResponse.Saved[klass.Id] as Klasse;
            Arkivdel savedSeries = transactionResponse.Saved[series.Id] as Arkivdel;

            Console.WriteLine(
                $"New classification system '{savedClassificationSystem.Tittel}' created. Temporary Id: {classificationSystem.Id}. Permanent Id: {savedClassificationSystem.Id}.");
            Console.WriteLine(
                $"New class '{savedClass.Tittel}' created. Temporary Id: {klass.Id}. Permanent Id: {savedClass.Id}.");
            Console.WriteLine(
                $"New series '{savedSeries.Tittel}' created. Temporary Id: {series.Id}. Permanent Id: {savedSeries.Id}.");

            return savedSeries.Id;
        }


        public void CreateListValues()
        {
            Console.WriteLine($"Create list values");
            NoarkClient client = this.documasterClients.GetNoarkClient();

            //Create a new administrative unit
            AdministrativEnhet administrativeUnit = new AdministrativEnhet("TK", "Test Kommune");
            AdministrativEnhet savedAdministrativeUnit = client.PutCodeListValue(administrativeUnit);

            //Create a new screening code
            Skjerming screeningCode = new Skjerming("N1", "Name", "Description", "Authority");
            Skjerming savedScreeningCode = client.PutCodeListValue(screeningCode);

            //Create a new  document type
            Dokumenttype documentType = new Dokumenttype("Tilbud", "Name");
            Dokumenttype newDocumentType = client.PutCodeListValue(documentType);
        }

        public void CreateBusinessSpecificMetaDataGroupsAndFields()
        {
            Console.WriteLine($"Create business-specific metadata structure");
            NoarkClient client = this.documasterClients.GetNoarkClient();

            string GROUP_ID = "gr-1";
            string STRING_FIELD_ID = "f-string";
            string DOUBLE_FIELD_ID = "f-double";
            string LONG_FIELD_ID = "f-long";

            //Create a business-specific metadata group
            MetadataGroupInfo group = new MetadataGroupInfo(GROUP_ID, "BSM Group Name", "BSM Group Description");
            MetadataGroupInfo savedGroup = client.PutBsmGroup(group);
            Console.WriteLine(
                $"New group: GroupId={savedGroup.GroupId}, GroupDescription={savedGroup.GroupDescription}, GroupName={savedGroup.GroupName}");

            //Create a new string field with predefined values "value 1", "value 2" and "value 3"
            MetadataFieldInfo fieldStr = new MetadataFieldInfo(STRING_FIELD_ID, "BSM Field String",
                "BSM Field Description", FieldType.String, new List<object>() {"value 1", "value 2", "value 3"});
            MetadataFieldInfo savedFieldStr = client.PutBsmField(GROUP_ID, fieldStr);
            Console.WriteLine(
                $"Created new field: FieldId={savedFieldStr.FieldId}, FieldType={savedFieldStr.FieldType}, FieldName={savedFieldStr.FieldName}, FieldValues={string.Join(",", savedFieldStr.FieldValues)}");

            //Create a new long field with predefined values 1 and 2
            MetadataFieldInfo fieldLong = new MetadataFieldInfo(LONG_FIELD_ID, "BSM Field Long",
                "BSM Field Description", FieldType.Long, new List<object>() {1L, 2L});
            MetadataFieldInfo savedFieldLong = client.PutBsmField(GROUP_ID, fieldLong);
            Console.WriteLine(
                $"Created new field: FieldId={savedFieldLong.FieldId}, FieldType={savedFieldLong.FieldType}, FieldName={savedFieldLong.FieldName}, FieldValues={string.Join(",", savedFieldLong.FieldValues)}");

            //Create a new double field with no predefined values
            MetadataFieldInfo fieldDouble = new MetadataFieldInfo(DOUBLE_FIELD_ID, "BSM Field Double",
                "BSM Field Description", FieldType.Double);
            MetadataFieldInfo savedFielDouble = client.PutBsmField(GROUP_ID, fieldDouble);
            Console.WriteLine(
                $"Created new field: FieldId={fieldDouble.FieldId}, FieldType={fieldDouble.FieldType}, FieldName={fieldDouble.FieldName}");

            //Get the business-specific metadata registry for a specific group
            BusinessSpecificMetadataInfo metadataInfo = client.BsmRegistry(GROUP_ID);

            Console.WriteLine("BusinessSpecificMetadataInfo:");
            //Print the registry for this group
            foreach (MetadataGroupInfo groupInfo in metadataInfo.Groups)
            {
                Console.WriteLine(
                    $"GroupInfo: GroupId={groupInfo.GroupId}, GroupName={groupInfo.GroupName}");
                foreach (MetadataFieldInfo fieldInfo in groupInfo.Fields)
                {
                    Console.WriteLine(
                        $" ---- FieldInfo: FieldId={fieldInfo.FieldId}, FieldType={fieldInfo.FieldType}, FieldName={fieldInfo.FieldName}");
                }
            }
        }
    }
}
