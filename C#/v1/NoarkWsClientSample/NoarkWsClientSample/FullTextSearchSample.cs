using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Documaster.WebApi.Client.Noark5.Client;

namespace NoarkWsClientSample
{
    public class FullTextSearchSample
    {
        private readonly DocumasterClients documasterClients;

        public FullTextSearchSample(DocumasterClients documasterClients)
        {
            this.documasterClients = documasterClients;
        }

        public void Search()
        {
            /**
             * Search for a free text string in index documents with the specified doctype.
             * An index document is a set of fields and their corresponding values stored in a search index.
             * Each index document has a doctype which corresponds to a set of Noark 5 fields stored in the index document.
             * The supported doctypes are Tekst, Arkivdel, Mappe, Registrering and Korrespondansepart.
             * Search fields are specific to each object type (eg. tittel in Arkivdel, klasseIdent and tittel in Klass etc.)
             */

            NoarkClient client = this.documasterClients.GetNoarkClient();

            int offset = 0;
            bool hasMoreResults = true;
            int pageSize = 30;
            int resultsCount = 0;

            List<SearchResponse> searchResults = new List<SearchResponse>();

            while (hasMoreResults)
            {
                //Search for the string "Test" 

                SearchResponse searchResponse = client.Search(Doctype.Tekst, "Test")
                    .SetLimit(pageSize)
                    .SetOffset(offset)
                    .Execute();

                searchResults.Add(searchResponse);
                resultsCount += searchResponse.Results.Count;
                offset += pageSize;
                hasMoreResults = resultsCount < searchResponse.Total;
            }

            foreach (SearchResponse searchResponse in searchResults)
            {
                // Each search result includes the IDs of the Noark 5 objects whose fields are included in the index document
                // as well as highlighted snippets from matching fields.

                foreach (SearchResult searchResult in searchResponse.Results)
                {
                    Console.WriteLine($"Object Ids: {string.Join(", ", searchResult.Ids.ToArray())}");
                    Console.WriteLine("Highlights:");
                    foreach (string matchingField in searchResult.Highlights.Keys)
                    {
                        Console.WriteLine(
                            $"Highlights for field {matchingField}: {string.Join(", ", searchResult.Highlights[matchingField].ToArray())}");
                    }
                }

                foreach (Facet facet in searchResponse.Facets)
                {
                    Console.WriteLine($"Facet field: {facet.Field}");
                    foreach (string facetValue in facet.Values.Keys)
                    {
                        Console.WriteLine($"Facet value: {facetValue}. Number of index documents with that value: {facet.Values[facetValue]}.");
                    }
                }
            }
        }
    }
}
