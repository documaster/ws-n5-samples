## Documaster Noark5 client sample code

This sample code uses the Documaster NoarkClient (a client library available through NuGet) to demonstrate search and submission of data.  
The **SystemInitializationSample.cs** class shows how to setup the system for initial use, i.e create fonds, series, code values etc.   
The **IntegrationSample** class demonstrates integration code as recommended by Documaster. It includes searching for existing entities (series, classes), creating of secondary classes and submitting data in a single transaction.  
The **SignOffSample.cs** class shows how to perform sign offs.  
The **FinalizationSample.cs** class demonstrates how to finalize entities of different types.  
The **QuerySample.cs** class shows how to query the system for data.  

### Setup instructions

The sample is contained in a .NET 4.7 Console application. You need to restore the NuGet packages in this project to be able to compile it.
The Main method in Program.cs runs all examples. Feel free to comment or delete the lines to run only specific examples.  

The following arguments are required:

* --idpaddr: The address of the identity provider services for the particular RMS instances (ex: "http://client.documaster.tech/idp/oauth2/" 
* --clientid: The OAuth client_id value.
* --clientsecret: The OAuth client_secret value.
* --username: The RMS username.
* --password The RMS user password.
* --addr: The address of Documaster's web services (ex: "http://client.documaster.tech:8083")
* --testfile1: A valid path to a test file to be uploaded to the RMS as main document.
* --testfile2: A valid path to a test file to be uploaded to the RMS as an attachment.

### Requirements

To run this sample code you need Documaster Archive version 2.16.0 or higher. In the sample, we authenticate to the RMS with the Oauth2HttpClient which is another public NuGet client library. The Oauth2HttpClient is a small OAuth2 client and we use it to obtain an authentication token from Documaster's own Identity Provider services. By default, the token expires in 60 minutes and needs to refreshed after that. This is why the **NoarkClient** and the **Oauth2HttpClient** instances in this sample are kept in a separate class called **DocumasterClients** which is responsible for getting and refreshing the token.



