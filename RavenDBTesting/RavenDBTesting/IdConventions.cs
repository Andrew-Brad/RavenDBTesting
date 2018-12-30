using System;
using System.Threading.Tasks;

namespace RavenDBTesting
{
    // All Id's in RavenDB are strings
    public static class IdConventions
    {
        /// <summary>
        /// Strategy which describes that a TeaProfile's Id is its collection name with its well known name.
        /// </summary>        
        public const string TeaProfileCollectionPrefix = @"TeaProfiles/";
        public static Func<string, TeaProfile, Task<string>> TeaNameIdStrategy => (dbname, profile) =>
                      Task.FromResult(string.Format("{0}{1}", TeaProfileCollectionPrefix, profile.Name));        
    }
}
