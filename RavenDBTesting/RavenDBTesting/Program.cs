using Raven.Client.Documents;
using System;
using static AB.Extensions.ConsoleExtensions;

namespace RavenDBTesting
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteLineWithColor("App started.", ConsoleColor.Yellow);
            DocumentStore store = InitializeRavenDbDocumentStore();
            WriteLineWithColor("Document store initialized with " + store.Database + " database.", ConsoleColor.Yellow);


            Console.ReadKey();
        }

        public static DocumentStore InitializeRavenDbDocumentStore()
        {
            DocumentStore store = new DocumentStore()
            {
                Urls = new string[] { "http://192.168.1.194:8080" },
                Database = "TestConsoleApp"
            };

            //store.Conventions.CustomizeJsonSerializer = AddCustomConverters;
            //store.Conventions.RegisterAsyncIdConvention<CardTemplate>(IdConventions.CardTemplateIdStrategy);
            // All customizations need to be set before DocumentStore.Initialize() is called.
            // https://ravendb.net/docs/article-page/4.0/csharp/client-api/configuration/conventions
            store.Initialize();
            return store;
        }
    }
}
