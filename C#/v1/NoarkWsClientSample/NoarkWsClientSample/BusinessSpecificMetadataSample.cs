using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Documaster.WebApi.Client.Noark5.Client;
using Documaster.WebApi.Client.Noark5.NoarkEntities;

namespace NoarkWsClientSample
{
    public class BusinessSpecificMetadataSample
    {
        private readonly DocumasterClients documasterClients;

        public BusinessSpecificMetadataSample(DocumasterClients documasterClients)
        {
            this.documasterClients = documasterClients;
        }

        public void GetBusinessSpecificMetadataRegistry()
        {
            Console.WriteLine($"Get business-specific metadata registry");

            NoarkClient client = this.documasterClients.GetNoarkClient();

            BusinessSpecificMetadataInfo info = client.BsmRegistry();

            foreach (MetadataGroupInfo metadataGroupInfo in info.Groups)
            {
                Console.WriteLine($"Group: {metadataGroupInfo.GroupName}");
                foreach (MetadataFieldInfo metadataFieldInfo in metadataGroupInfo.Fields)
                {
                    Console.WriteLine($"Field: Name={metadataFieldInfo.FieldName}, Type={metadataFieldInfo.FieldType}");
                    if(metadataFieldInfo.FieldValues != null)
                    {
                        //the field has predefined values
                        Console.WriteLine($"Field values: [{String.Join(",", metadataFieldInfo.FieldValues.ToArray())}]");
                    }
                }
            }
        }

        public void CrudOperationsWithBusinessSpecificMetadata()
        {
            Console.WriteLine($"Create, update and delete business-specific metadata groups and fields");

            NoarkClient client = this.documasterClients.GetNoarkClient();

            //Create group
            MetadataGroupInfo group = new MetadataGroupInfo("group-applications", "Applications", "Business-specific metadata for applications");
            MetadataGroupInfo savedGroup = client.PutBsmGroup(group);
            Console.WriteLine($"Created group with group identifier {savedGroup.GroupId}");

            //Create fields:
            MetadataFieldInfo fieldAppliactionName = new MetadataFieldInfo("app-name", "Application name", "Application name", FieldType.String);
            MetadataFieldInfo fieldAppliactionParticipants = new MetadataFieldInfo("app-participants", "Application participants", "Application participants", FieldType.String);
            MetadataFieldInfo fieldAppliactionType = new MetadataFieldInfo("app-type", "Application type", "Application type", FieldType.Double, new List<object> { 1, 2, 3 });
            MetadataFieldInfo fieldAppliactionSalary = new MetadataFieldInfo("app-salary", "Application salary", "Application salary", FieldType.Long);
            MetadataFieldInfo fieldAppliactionDate = new MetadataFieldInfo("app-date", "Application date", "Application date", FieldType.Timestamp);
            MetadataFieldInfo fieldAppliactionSecret = new MetadataFieldInfo("app-secret", "Application secret", "Application secret", FieldType.Encrypted);

            MetadataFieldInfo savedFieldAppliactionName = client.PutBsmField(savedGroup.GroupId, fieldAppliactionName);
            MetadataFieldInfo savedFieldAppliactionParticipants = client.PutBsmField(savedGroup.GroupId, fieldAppliactionParticipants);
            MetadataFieldInfo savedFieldAppliactionType = client.PutBsmField(savedGroup.GroupId, fieldAppliactionType);
            MetadataFieldInfo savedFieldAppliactionSalary = client.PutBsmField(savedGroup.GroupId, fieldAppliactionSalary);
            MetadataFieldInfo savedFieldAppliactionDate = client.PutBsmField(savedGroup.GroupId, fieldAppliactionDate);
            MetadataFieldInfo savedFieldAppliactionSecret = client.PutBsmField(savedGroup.GroupId, fieldAppliactionSecret);

            Console.WriteLine($"Created fields with filed identifiers {savedFieldAppliactionName.FieldId}, " +
                $"{savedFieldAppliactionType.FieldId}, {savedFieldAppliactionDate.FieldId}, {savedFieldAppliactionSalary.FieldId} and {savedFieldAppliactionSecret.FieldId}");

            //Fetch and update group
            BusinessSpecificMetadataInfo info = client.BsmRegistry(savedGroup.GroupId);
            MetadataGroupInfo foundGroup = info.Groups.First();
            //Remove one of the fields from the group
            foundGroup.Fields.RemoveAt(3);
            client.PutBsmGroup(foundGroup);

            //Update a field with predefined values - add one more value
            savedFieldAppliactionType.FieldValues.Add(4);
            client.PutBsmField(savedGroup.GroupId, savedFieldAppliactionType);


            //Delete a field
            client.DeleteBsmField(savedGroup.GroupId, savedFieldAppliactionSalary.FieldId);

            //Delete a group
            client.DeleteBsmGroup(savedGroup.GroupId);
        }

        public void AddAndUpdateBusinessSpecificMetadataToCaseFile(string caseFileTitle)
        {
            //This example assumes the existence of a bsm group with identifier "group-applications"
            //and of several bsm fields.

            Console.WriteLine("Add and update business-specific metadata to case file");
            NoarkClient client = this.documasterClients.GetNoarkClient();

            //Find the file
            Saksmappe saksmappe =
                client.Query<Saksmappe>("tittel=@title", 1)
                .AddQueryParam("@title", caseFileTitle)
                .Execute()
                .Results
                .First();

            //Set application name, date, type and secret as business-specific metadata to a case file
            saksmappe.VirksomhetsspesifikkeMetadata.AddBsmFieldValues("group-applications", "app-name", "Application for kindergarten place");
            saksmappe.VirksomhetsspesifikkeMetadata.AddBsmFieldValues("group-applications", "app-participants",  "Alice Smith", "John Doe");
            saksmappe.VirksomhetsspesifikkeMetadata.AddBsmFieldValues("group-applications", "app-date", DateTime.Now);
            saksmappe.VirksomhetsspesifikkeMetadata.AddBsmFieldValues("group-applications", "app-type", 1);
            saksmappe.VirksomhetsspesifikkeMetadata.AddBsmFieldValues("group-applications", "app-secret", "some encrypted content here");

            TransactionResponse transactionResponse = client.Transaction()
                .Save(saksmappe)
                .Commit();

            Saksmappe savedCaseFile = transactionResponse.Saved[saksmappe.Id] as Saksmappe;

            Console.WriteLine("Added business-specific metadata fields to case file");
            foreach(string groupId in savedCaseFile.VirksomhetsspesifikkeMetadata.Keys)
            {
                Console.WriteLine($"Group with group identifier {groupId}");
                foreach(string fieldId in savedCaseFile.VirksomhetsspesifikkeMetadata[groupId].Keys)
                {
                    BsmFieldValues fieldValues = savedCaseFile.VirksomhetsspesifikkeMetadata[groupId][fieldId];
                    Console.WriteLine($@"Field with field identifier {fieldId}, type {fieldValues.Type} 
                            and values [{String.Join(", ", fieldValues.Values)}]");
                }
            }

            //Delete all field values from the case file
            savedCaseFile.VirksomhetsspesifikkeMetadata.DeleteBsmField("group-applications", "app-secret");

            //Delete a field value only
            savedCaseFile.VirksomhetsspesifikkeMetadata.DeleteBsmFieldValue("group-applications", "app-participants", "Alice Smith");

            //Add a new value
            savedCaseFile.VirksomhetsspesifikkeMetadata.UpdateBsmFieldValues("group-applications", "app-type", 2);

            transactionResponse = client.Transaction()
                .Save(savedCaseFile)
                .Commit();

            savedCaseFile = transactionResponse.Saved[saksmappe.Id] as Saksmappe;

            Console.WriteLine("Update business-specific metadata fields of case file");
            foreach (string groupId in savedCaseFile.VirksomhetsspesifikkeMetadata.Keys)
            {
                Console.WriteLine($"Group with group identifier {groupId}");
                foreach (string fieldId in savedCaseFile.VirksomhetsspesifikkeMetadata[groupId].Keys)
                {
                    BsmFieldValues fieldValues = savedCaseFile.VirksomhetsspesifikkeMetadata[groupId][fieldId];
                    Console.WriteLine($@"Field with field identifier {fieldId}, type {fieldValues.Type} 
                            and values [{String.Join(", ", fieldValues.Values)}]");
                }
            }
        }
    }
}
