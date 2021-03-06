﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Session;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using static AB.Extensions.ConsoleExtensions;
using static RavenDBTesting.RavenIndexes;

namespace RavenDBTesting
{
    class Program
    {
        const string DatabaseName = "RavenDBTeaCollection";
        static void Main(string[] args)
        {
            WriteLineWithColor("App started.", ConsoleColor.Yellow);
            DocumentStore store = InitializeRavenDbDocumentStore();
            WriteLineWithColor("Document store initialized with " + store.Database + " database.", ConsoleColor.Yellow);

            #region Delete Database

            store.Maintenance.Server.Send(new DeleteDatabasesOperation(store.Database, hardDelete: true));

            #endregion

            #region Create Database

            store.Maintenance.Server.Send(new CreateDatabaseOperation(new DatabaseRecord(DatabaseName)));

            // Prepare indexes (multiple ways to deploy these)
            // new TeaProfile_CaffeineIndex().Execute(store);

            // All classes that inherit from AbstractIndexCreationTask can be deployed at once using one of IndexCreation.CreateIndexes method overloads.
            IndexCreation.CreateIndexes(typeof(Program).Assembly, store);

            #endregion

            #region Store & SaveChanges

            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                List<TeaProfile> teaProfiles = GetProfiles();
                foreach (var profile in teaProfiles)
                {
                    var collectionName = store.Conventions.GetCollectionName(profile);
                    // data only staged into the session, not in the database yet                                       
                    session.Store(profile); // also id and change vector overloads!
                    WriteLineWithColor($"Saved {profile.Name} to session.", ConsoleColor.DarkGreen);
                }

                Stopwatch saveChangesStopwatch = new Stopwatch();
                WriteLineWithColor($"Saving session to database...", ConsoleColor.Yellow);
                saveChangesStopwatch.Start();
                session.SaveChanges();
                saveChangesStopwatch.Stop();
                WriteLineWithColor($"Session persisted database in {saveChangesStopwatch.ElapsedMilliseconds} ms.", ConsoleColor.Green);
            }

            #endregion

            #region Load - https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/loading-entities#load

            WriteLineWithColor($"Moving to Load()", ConsoleColor.Blue);
            TeaProfile loadProfileById = null;
            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                string idPrefix = store.Conventions.FindCollectionName(typeof(TeaProfile));
                loadProfileById = session.Load<TeaProfile>(idPrefix + "/" + "Earl Grey");
                WriteLineWithColor($"Loaded {loadProfileById.Name}.  It has {loadProfileById.CaffeineMilligrams} mg of caffeine!", ConsoleColor.Green);
            }

            #endregion

            #region Load Multiple - https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/loading-entities#load---multiple-entities

            WriteLineWithColor($"Moving to LoadMultiple()", ConsoleColor.Blue);
            Dictionary<string, TeaProfile> loadMultipleProfiles = null;
            List<string> idsToFetch = TeaNamesDictionary.Values.ToList();
            for (int i = 0; i < idsToFetch.Count; i++)
            {
                // RavenDB documents its Id building format as such https://ravendb.net/docs/article-page/4.1/csharp/client-api/configuration/identifier-generation/global#identitypartsseparator
                idsToFetch[i] = store.Conventions.FindCollectionName(typeof(TeaProfile)) + store.Conventions.IdentityPartsSeparator + idsToFetch[i];
            }

            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                Stopwatch loadStopwatch = new Stopwatch();
                loadStopwatch.Start();
                loadMultipleProfiles = session.Load<TeaProfile>(idsToFetch);
                loadStopwatch.Stop();
                WriteLineWithColor($"Loaded {loadMultipleProfiles.Count} profiles in {loadStopwatch.ElapsedMilliseconds} ms. Together, they have a total of {loadMultipleProfiles.Values.Sum(x => x.CaffeineMilligrams)} mg of caffeine!", ConsoleColor.Green);
            }

            #endregion

            #region Load StartingWith - https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/loading-entities#load---multiple-entities

            // Think of this as a way to stream/page the entire dataset with no conditional criteria
            WriteLineWithColor($"Moving to Load StartingWith()", ConsoleColor.Blue);
            TeaProfile[] loadProfilesStartingWith = null;
            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                string prefix = store.Conventions.FindCollectionName(typeof(TeaProfile));
                Stopwatch loadStopwatch = new Stopwatch();
                loadStopwatch.Start();
                loadProfilesStartingWith = session
                    .Advanced
                    .LoadStartingWith<TeaProfile>(prefix, null, 0, 50);
                loadStopwatch.Stop();
                WriteLineWithColor($"Loaded {loadProfilesStartingWith.Count()} profiles in {loadStopwatch.ElapsedMilliseconds} ms. Together, they have a total of {loadProfilesStartingWith.Sum(x => x.CaffeineMilligrams)} mg of caffeine!", ConsoleColor.Green);
            }

            #endregion

            #region Basic Document Query - https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/querying/document-query/what-is-document-query#example-i---basic

            // Without where clauses, this is effectively the previous query, SELECT all docs
            WriteLineWithColor($"Moving to Basic Document Query", ConsoleColor.Blue);
            List<TeaProfile> docQueryAllProfiles = null;
            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                Stopwatch loadStopwatch = new Stopwatch();
                loadStopwatch.Start();
                docQueryAllProfiles = session
                    .Advanced
                    .DocumentQuery<TeaProfile>()
                    .WhereGreaterThan(x => x.CaffeineMilligrams, 5)
                    .ToList();
                loadStopwatch.Stop();
                WriteLineWithColor($"Loaded {docQueryAllProfiles.Count} profiles that have caffeine greater than 5 in {loadStopwatch.ElapsedMilliseconds} ms. Together, they have a total of {docQueryAllProfiles.Sum(x => x.CaffeineMilligrams)} mg of caffeine!", ConsoleColor.Green);
            }

            // More custom methods at https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/querying/document-query/what-is-document-query#custom-methods-and-extensions

            #endregion

            #region Advanced Document Query - https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/querying/document-query/what-is-document-query#example-i---basic

            // Expressing complex queries natively in the C# - eventually these turn into dedicated indexes server-side
            WriteLineWithColor($"Moving to Advanced Document Query", ConsoleColor.Blue);
            List<TeaProfile> docQuerySpecificProfiles = null;
            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                Stopwatch loadStopwatch = new Stopwatch();
                loadStopwatch.Start();
                // tea profiles that aren't white tea, and don't have caffeine above 5
                docQuerySpecificProfiles = session
                    .Advanced
                    .DocumentQuery<TeaProfile>()
                    .WhereNotEquals(x => x.TeaColor, TeaProfile.TeaColorEnum.White)
                    .AndAlso()
                    .Not
                    .OpenSubclause()
                    .WhereGreaterThan(x => x.CaffeineMilligrams, 5)
                    .CloseSubclause()
                    .ToList();
                loadStopwatch.Stop();
                WriteLineWithColor($"Loaded {docQuerySpecificProfiles.Count} tea profiles that aren't white tea, and don't have" +
                    $" caffeine greater than 5 in {loadStopwatch.ElapsedMilliseconds} ms. {string.Join(",", docQuerySpecificProfiles.Select(x => x.Name))} made " +
                    $"the list. Together, they have a total of {docQuerySpecificProfiles.Sum(x => x.CaffeineMilligrams)} mg of caffeine!", ConsoleColor.Green);
            }

            // More custom methods at https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/querying/document-query/what-is-document-query#custom-methods-and-extensions

            #endregion

            #region Query against index - https://ravendb.net/docs/article-page/4.1/csharp/indexes/what-are-indexes#basic-example

            WriteLineWithColor($"Moving to Query against index", ConsoleColor.Blue);
            List<TeaProfile> indexedQueryResults = null;
            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                Stopwatch loadStopwatch = new Stopwatch();
                loadStopwatch.Start();
                indexedQueryResults = session
                    .Query<TeaProfile, TeaProfile_CaffeineIndex>()
                    .Where(x => x.CaffeineMilligrams < 3)
                    .ToList();
                loadStopwatch.Stop();
                // or with the advanced stuff
                indexedQueryResults = session
                    .Advanced
                    .DocumentQuery<TeaProfile, TeaProfile_CaffeineIndex>()
                    .WhereBetween(x => x.CaffeineMilligrams, 0, 3)
                    .ToList();

                WriteLineWithColor($"Loaded {indexedQueryResults.Count} profiles that have caffeine less than 5 in {loadStopwatch.ElapsedMilliseconds} ms. Together, they have a total of {indexedQueryResults.Sum(x => x.CaffeineMilligrams)} mg of caffeine!", ConsoleColor.Green);
            }

            // More custom methods at https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/querying/document-query/what-is-document-query#custom-methods-and-extensions

            #endregion

            #region Query with includes, no index

            WriteLineWithColor($"Moving to Query with include", ConsoleColor.Blue);
            TeaProfile teaProfileForCup = indexedQueryResults.First();
            CupOfTea cup1 = new CupOfTea() { PouredOn = DateTime.UtcNow, TeaProfileId = teaProfileForCup.Id, Temperature = 212 };
            CupOfTea cup2 = new CupOfTea() { PouredOn = DateTime.UtcNow, TeaProfileId = indexedQueryResults.First(x => x.CaffeineMilligrams > 1).Id, Temperature = 200 };

            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                Stopwatch loadStopwatch = new Stopwatch();
                loadStopwatch.Start();

                session.Store(cup1);
                session.Store(cup2);
                session.SaveChanges(); // roundtrip 1

                CupOfTea cup1db = session
                    .Include<CupOfTea>(x => x.TeaProfileId)
                    .Load<CupOfTea>(cup1.Id); // roundtrip skipped due to already being loaded in session

                // session will hold and track multiple entities instead of making the full roundtrip
                bool isProfileLoadedFromSession = session.Advanced.IsLoaded(teaProfileForCup.Id); // false

                TeaProfile includedProfile = session.Load<TeaProfile>(cup1db.TeaProfileId); // loading doc not in session, roundtrip 2

                // this now becomes true, but only 2 roundtrips made (1 for initial save and 1 for read)
                isProfileLoadedFromSession = session.Advanced.IsLoaded(teaProfileForCup.Id);
                int roundtrips = session.Advanced.NumberOfRequests;

                loadStopwatch.Stop();
                WriteLineWithColor($"Loaded document with Include() in {loadStopwatch.ElapsedMilliseconds} ms.", ConsoleColor.Green);
            }

            // More custom methods at https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/querying/document-query/what-is-document-query#custom-methods-and-extensions

            #endregion

            #region Multi doc query with multi include, many-to-many use case

            WriteLineWithColor($"Moving to multi doc query with multi include", ConsoleColor.Blue);
            // reusing objects from above, but instantiating fresh session
            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                Stopwatch loadStopwatch = new Stopwatch();
                loadStopwatch.Start();

                List<CupOfTea> allCupsOfTeaInDatabase = session
                    .Advanced
                    .DocumentQuery<CupOfTea>()
                    .Include(x => x.TeaProfileId)
                    //.Include<CupOfTea>(x => x.TeaProfileId)
                    .ToList();
                int roundtrips = session.Advanced.NumberOfRequests;

                // all associated docs in session, still only 1 roundtrip
                var associatedProfiles = session.Load<TeaProfile>(
                        allCupsOfTeaInDatabase
                        .Select(x => x.TeaProfileId)
                    .ToArray())
                    .ToList();

                roundtrips = session.Advanced.NumberOfRequests;

                loadStopwatch.Stop();
                WriteLineWithColor($"Loaded multiple documents with multi includes in {loadStopwatch.ElapsedMilliseconds} ms.", ConsoleColor.Green);
            }

            // More custom methods at https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/querying/document-query/what-is-document-query#custom-methods-and-extensions

            #endregion


            #region Interrogate document store conventions to assist with calling code

            string conventionCollectionName = store.Conventions.FindCollectionName(typeof(TeaProfile));
            string idPropertyName = store.Conventions.FindIdentityPropertyNameFromCollectionName("TeaProfiles");
            var idPropertyMemberInfo = store.Conventions.GetIdentityProperty(typeof(TeaProfile));
            string idSeparator = store.Conventions.IdentityPartsSeparator; // can also be changed!      

            #endregion

            #region LoadStartingWith - multiple executions when underlying Change Vector is different
            #endregion

            Console.ReadKey();

            #region Delete Database

            store.Maintenance.Server.Send(new DeleteDatabasesOperation(store.Database, hardDelete: true));

            #endregion
        }

        private static List<TeaProfile> GetProfiles()
        {
            TeaProfile[] profiles = new TeaProfile[7];
            for (int i = 1; i <= 7; i++)
            {
                var prof = new TeaProfile((TeaProfile.TeaColorEnum)i, TeaNamesDictionary[i]);
                prof.Name = TeaNamesDictionary[i];
                prof.CaffeineMilligrams = i;
                prof.TeaColor = (TeaProfile.TeaColorEnum)i;
                profiles[i - 1] = prof;
            }
            return profiles.ToList();
        }

        public static DocumentStore InitializeRavenDbDocumentStore()
        {
            DocumentStore store = new DocumentStore()
            {
                Urls = new string[] { "http://192.168.1.194:8080" },
                Database = DatabaseName
            };

            //store.Conventions.CustomizeJsonSerializer = AddCustomConverters;
            store.Conventions.RegisterAsyncIdConvention<TeaProfile>(IdConventions.TeaNameIdStrategy);
            store.Conventions.SaveEnumsAsIntegers = true;

            // All customizations need to be set before DocumentStore.Initialize() is called.
            // https://ravendb.net/docs/article-page/4.0/csharp/client-api/configuration/conventions
            store.Initialize();

            return store;
        }

        // hardcoded sample data
        public static Dictionary<int, string> TeaNamesDictionary { get; set; } = new Dictionary<int, string>()
        {
            {1,"Matcha" },
            {2, "Earl Grey" },
            {3, "White Peony" },
            {4, "Blood Orange" },
            {5, "Joker Tea" },
            {6, "Masala Chai" },
            {7, "Peach Oolong" }
        };
    }
}
