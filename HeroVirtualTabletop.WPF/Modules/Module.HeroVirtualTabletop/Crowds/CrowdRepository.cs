using Module.HeroVirtualTabletop.Library.Utility;
using Module.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.IO;
using System.Linq;
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
                        AutoResetEvent e = new AutoResetEvent(false);
                        events.Add(e);
                        if (mutexes.Count > 0)
                            Mutex.WaitAll(mutexes.ToArray());
                        Mutex m = new Mutex(true);
                        mutexes.Add(m);

                        List<CrowdModel> crowdCollection = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(crowdRepositoryPath);
                        if (crowdCollection == null)
                            crowdCollection = new List<CrowdModel>();
                        TakeBackup(); // Take backup of valid data file from last execution
                        this.getCrowdCollectionCompleted(crowdCollection);

                        m.ReleaseMutex();
                        mutexes.Remove(m);
                        e.Set();
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
                            AutoResetEvent e = new AutoResetEvent(false);
                            events.Add(e);
                            if (mutexes.Count > 0)
                                Mutex.WaitAll(mutexes.ToArray());
                            Mutex m = new Mutex(true);
                            mutexes.Add(m);

                            Helper.SerializeObjectAsJSONToFile(crowdRepositoryPath, crowdCollection);
                            this.saveCrowdCollectionCompleted();

                            m.ReleaseMutex();
                            mutexes.Remove(m);
                            e.Set();
                            events.Remove(e);
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
    }
}

