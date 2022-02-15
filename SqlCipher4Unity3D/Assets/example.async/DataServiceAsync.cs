using System.Threading.Tasks;
using SqlCipher4Unity3D;
using UnityEngine;

#if !UNITY_EDITOR
using System.Collections;
using System.IO;
#endif
namespace example.async
{
    using Cysharp.Threading.Tasks;
    using SqlCipher4Unity3D.UniTaskIntegration;

    public class DataServiceAsync
    {
        private readonly SQLiteAsyncConnection _connection;
        public DataServiceAsync(string DatabaseName)
        {
#if UNITY_EDITOR
            string dbPath = string.Format(@"Assets/StreamingAssets/{0}", DatabaseName);
#else
            // check if file exists in Application.persistentDataPath
            string filepath = string.Format("{0}/{1}", Application.persistentDataPath, DatabaseName);

            if (!File.Exists(filepath))
            {
                Debug.Log("Database not in Persistent path");
                // if it doesn't ->
                // open StreamingAssets directory and load the db ->

#if UNITY_ANDROID
                WWW loadDb =
     new WWW ("jar:file://" + Application.dataPath + "!/assets/" + DatabaseName); // this is the path to your StreamingAssets in android
                while (!loadDb.isDone) { } // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
                // then save to Application.persistentDataPath
                File.WriteAllBytes (filepath, loadDb.bytes);
#elif UNITY_IOS
                string loadDb =
     Application.dataPath + "/Raw/" + DatabaseName; // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy (loadDb, filepath);
#elif UNITY_WP8
                string loadDb =
     Application.dataPath + "/StreamingAssets/" + DatabaseName; // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy (loadDb, filepath);
    
#elif UNITY_WINRT
                string loadDb =
     Application.dataPath + "/StreamingAssets/" + DatabaseName; // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy (loadDb, filepath);
#elif UNITY_STANDALONE_OSX
                string loadDb =
     Application.dataPath + "/Resources/Data/StreamingAssets/" + DatabaseName; // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);
#else
                string loadDb =
     Application.dataPath + "/StreamingAssets/" + DatabaseName; // this is the path to your StreamingAssets in iOS
                // then save to Application.persistentDataPath
                File.Copy(loadDb, filepath);
#endif

                Debug.Log("Database written");
            }

            var dbPath = filepath;
#endif
            _connection = new SQLiteAsyncConnection(dbPath, "password");
            Debug.Log("Final PATH: " + dbPath);
        }
        ~DataServiceAsync()
        {
            #if SQLITEASYNC_UNITASK
            _connection.CloseAsync();
            #else
            _connection.CloseAsync().Wait();
            #endif

        }

        public async Task CreateDB()
        {
            await _connection.DropTableAsync<Person>();
            await _connection.CreateTableAsync<Person>();

            await _connection.InsertAllAsync(new[]
            {
                new Person
                {
                    Id = 1,
                    Name = "Tom",
                    Surname = "Perez",
                    Age = 56
                },
                new Person
                {
                    Id = 2,
                    Name = "Fred",
                    Surname = "Arthurson",
                    Age = 16
                },
                new Person
                {
                    Id = 3,
                    Name = "John",
                    Surname = "Doe",
                    Age = 25
                },
                new Person
                {
                    Id = 4,
                    Name = "Roberto",
                    Surname = "Huertas",
                    Age = 37
                }
            });
        }

        public AsyncTableQuery<Person> GetPersons()
        {
            return _connection.Table<Person>();
        }

        public AsyncTableQuery<Person> GetPersonsNamedRoberto()
        {
            return _connection.Table<Person>().Where(x => x.Name == "Roberto");
        }
        
        #if SQLITEASYNC_UNITASK
        public UniTask<Person> GetJohnny()
        {
            return _connection.Table<Person>().Where(person => person.Name == "Johnny").FirstOrDefaultAsync();
        }

        #else
        public Task<Person> GetJohnny()
        {
            return _connection.Table<Person>().Where(x => x.Name == "Johnny").FirstOrDefaultAsync();
        }

        #endif
        public async Task<Person> CreatePerson()
        {
            Person p = new Person
            {
                Name = "Johnny",
                Surname = "Mnemonic",
                Age = 21
            };
            await _connection.InsertAsync(p);
            return p;
        }
    }
}