using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using System;
using System.Collections.Generic;
using static AB.Extensions.ConsoleExtensions;

namespace RavenDBTesting
{
    class Program
    {
        const string DatabaseName = "RavenDBTesting";
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

            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                for (int i = 1; i <= 7; i++)
                {
                    //var prof = new TeaProfile();
                    var prof = new TeaProfile((TeaProfile.TeaColorEnum)i, TeaNamesDictionary[i]);
                    prof.Name = TeaNamesDictionary[i];
                    prof.CaffeineMilligrams = 34;
                    prof.TeaColor = (TeaProfile.TeaColorEnum)i;

                    session.Store(prof); // also id and change vector overloads!
                    WriteLineWithColor($"Saved {prof.Name} to database.", ConsoleColor.Green);
                }

                // data only staged into the session, not in the database yet
                session.SaveChanges();
            }

            WriteLineWithColor($"Moving to Load()", ConsoleColor.Yellow);

            using (IDocumentSession session = store.OpenSession(new SessionOptions()))
            {
                TeaProfile profile = session.Load<TeaProfile>("Earl Grey");
                WriteLineWithColor($"Loaded {profile.Name}", ConsoleColor.Blue);
            }




            Console.ReadKey();

            #region Delete Database

            store.Maintenance.Server.Send(new DeleteDatabasesOperation(store.Database, hardDelete: true));

            #endregion
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

        // temp variables
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
