using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static AB.Extensions.ConsoleExtensions;

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

            #endregion

            #region Store & SaveChanges

            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                List<TeaProfile> teaProfiles = GetProfiles();
                foreach (var profile in teaProfiles)
                {
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
                loadProfileById = session.Load<TeaProfile>("Earl Grey");
                WriteLineWithColor($"Loaded {loadProfileById.Name}.  It has {loadProfileById.CaffeineMilligrams} mg of caffeine!", ConsoleColor.Green);
            }

            #endregion

            #region Load Multiple - https://ravendb.net/docs/article-page/4.1/csharp/client-api/session/loading-entities#load---multiple-entities

            WriteLineWithColor($"Moving to LoadMultiple()", ConsoleColor.Blue);
            Dictionary<string,TeaProfile> loadMultipleProfiles = null;
            IEnumerable<string> idsToFetch = TeaNamesDictionary.Values.AsEnumerable();
            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                loadMultipleProfiles = session.Load<TeaProfile>(idsToFetch);
                WriteLineWithColor($"Loaded {loadMultipleProfiles.Count} profiles! Together, they have a total of {loadMultipleProfiles.Values.Sum(x => x.CaffeineMilligrams)} mg of caffeine!", ConsoleColor.Green);
            }

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
                prof.CaffeineMilligrams = 34;
                prof.TeaColor = (TeaProfile.TeaColorEnum)i;
                profiles[i-1] = prof;
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
            //store.Conventions.RegisterAsyncIdConvention<CardTemplate>(IdConventions.CardTemplateIdStrategy);
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
