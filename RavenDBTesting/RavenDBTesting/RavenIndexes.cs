using System.Linq;
using Raven.Client.Documents.Indexes;

namespace RavenDBTesting
{
    /// <summary>
    /// Queries are the ONLY way to satsfy queries in RavenDB.
    /// </summary>
    public class RavenIndexes
    {
        // https://ravendb.net/docs/article-page/4.1/csharp/indexes/what-are-indexes#basic-example
        public class TeaProfile_CaffeineIndex : AbstractIndexCreationTask<TeaProfile>
        {
            /* this essentially represents the fields in the index itself, not so much the results of the query
            / From the docs: A frequent mistake is to treat indexes as SQL Views, but they are not analogous. 
            / The result of a query for the given index is a full document, not only the fields that were indexed.
            */
            public TeaProfile_CaffeineIndex()
            {
                Map = teaprofiles => from profile in teaprofiles
                                   select new
                                   {
                                       CaffeineMilligrams = profile.CaffeineMilligrams                                       
                                   };                
            }
        }
    }
}
