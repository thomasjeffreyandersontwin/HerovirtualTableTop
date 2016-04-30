using Module.HeroVirtualTabletop.Models;
using Module.HeroVirtualTabletop.Utility;
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
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Repositories
{
    
    public class CrowdRepository : ICrowdRepository
    {
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

            System.Threading.ThreadPool.QueueUserWorkItem
            (new System.Threading.WaitCallback
                (
                    delegate(object state)
                    {
                        List<CrowdModel> crowdCollection = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(crowdRepositoryPath);
                        if (crowdCollection == null)
                            crowdCollection = new List<CrowdModel>();
                        this.getCrowdCollectionCompleted(crowdCollection);
                    }
                )
            );
        }

        private Action saveCrowdCollectionCompleted;
        public void SaveCrowdCollection(Action SaveCrowdCollectionCompleted, List<CrowdModel> crowdCollection)
        {
            this.saveCrowdCollectionCompleted = SaveCrowdCollectionCompleted;

            System.Threading.ThreadPool.QueueUserWorkItem
            (new System.Threading.WaitCallback
                (
                    delegate(object state)
                    {
                        Helper.SerializeObjectAsJSONToFile(crowdRepositoryPath, crowdCollection);
                        this.saveCrowdCollectionCompleted();
                    }
                )
            );
        }

        public CrowdRepository()
        {
            crowdRepositoryPath = Path.Combine(Module.Shared.Settings.Default.CityOfHeroesGameDirectory, Constants.GAME_DATA_FOLDERNAME, Constants.GAME_CROWD_REPOSITORY_FILENAME);
        }

        //public void SaveCrowdCollection(List<CrowdModel> crowdCollection)
        //{
        //    Helper.SerializeObjectAsJSONToFile(crowdRepoFileName, crowdCollection);
        //}

        //public List<CrowdModel> GetCrowdCollection()
        //{
        //    List<CrowdModel> crowdCollection = Helper.GetDeserializedJSONFromFile<List<CrowdModel>>(crowdRepoFileName);
        //    if (crowdCollection == null)
        //        crowdCollection = new List<CrowdModel>();

        //    return crowdCollection;
        //}
    }
}

