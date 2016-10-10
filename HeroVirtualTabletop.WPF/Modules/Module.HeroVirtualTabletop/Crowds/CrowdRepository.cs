using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Crowds
{
    public interface ICrowdRepository
    {
        string CrowdRepositoryPath
        {
            get;
        }
        void GetCrowdCollection(Action<List<CrowdModel>> GetCrowdCollectionCompleted);
        void SaveCrowdCollection(Action SaveCrowdCollectionCompleted, List<CrowdModel> crowdCollection);
        List<CrowdModel> LoadDefaultCrowdMembers();
    }

    public class CrowdRepository : ICrowdRepository
    {
        List<Mutex> mutexes;
        List<AutoResetEvent> events;

        private string crowdRepositoryPath;
        public string CrowdRepositoryPath
        {
            get
            {
                return crowdRepositoryPath;
            }
            set
            {
                crowdRepositoryPath = value;
            }
        }

        private Action<List<CrowdModel>> getCrowdCollectionCompleted;
        public void GetCrowdCollection(Action<List<CrowdModel>> GetCrowdCollectionCompleted)
        {
            this.getCrowdCollectionCompleted = GetCrowdCollectionCompleted;

            ThreadPool.QueueUserWorkItem
                (new WaitCallback(
                    delegate (object state)
                    {
                        List<CrowdModel> crowdCollection = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(crowdRepositoryPath);
                        if (crowdCollection == null)
                            crowdCollection = new List<CrowdModel>();
                        TakeBackup(); // Take backup of valid data file from last execution
                        this.getCrowdCollectionCompleted(crowdCollection);
                    }));
        }

        private Action saveCrowdCollectionCompleted;
        public void SaveCrowdCollection(Action SaveCrowdCollectionCompleted, List<CrowdModel> crowdCollection)
        {
            this.saveCrowdCollectionCompleted = SaveCrowdCollectionCompleted;

            ThreadPool.QueueUserWorkItem
                (new WaitCallback(
                    delegate (object state)
                        {
                            Helper.SerializeObjectAsJSONToFile(crowdRepositoryPath, crowdCollection);
                            
                            this.saveCrowdCollectionCompleted();
                        }));
        }

        public void WaitCompletion()
        {
            WaitHandle.WaitAll(events.ToArray());
        }

        public CrowdRepository()
        {
            crowdRepositoryPath = Path.Combine(Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_CROWD_REPOSITORY_FILENAME);
            mutexes = new List<Mutex>();
            events = new List<AutoResetEvent>();
        }

        private void TakeBackup()
        {
            string backupDir = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_DATA_BACKUP_FOLDERNAME);
            if (!Directory.Exists(backupDir))
                Directory.CreateDirectory(backupDir);
            string backupFilePath = Path.Combine(backupDir, "CrowdRepository_Backup" + String.Format("{0:MMddyyyy}", DateTime.Today) + ".data");
            if(!File.Exists(backupFilePath))
            {
                File.Copy(crowdRepositoryPath, backupFilePath, true);
            }

        }

        public List<CrowdModel> LoadDefaultCrowdMembers()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            List<CrowdModel> crowdCollection = new List<CrowdModel>();
            string resName = "Module.HeroVirtualTabletop.Resources.DefaultCharactersWithAbilities.data";
            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(resName)))
            {
                using (JsonReader reader = new JsonTextReader(sr))
                {

                    serializer.PreserveReferencesHandling = PreserveReferencesHandling.Objects;
                    serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                    serializer.Formatting = Formatting.Indented;
                    serializer.TypeNameHandling = TypeNameHandling.Objects;

                    crowdCollection = serializer.Deserialize<List<CrowdModel>>(reader);
                }
            }

            return crowdCollection;
        }
    }
}

