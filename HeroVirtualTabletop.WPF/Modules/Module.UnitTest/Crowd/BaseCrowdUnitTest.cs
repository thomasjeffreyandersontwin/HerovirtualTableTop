using Module.HeroVirtualTabletop.DomainModels;
using Module.HeroVirtualTabletop.Models;
using Module.HeroVirtualTabletop.Repositories;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.UnitTest
{
    public class BaseCrowdUnitTest : BaseTest
    {
        protected Mock<ICrowdRepository> crowdRepositoryMock = new Mock<ICrowdRepository>();
        protected List<CrowdModel> crowdModelList;

        protected void InitializeCrowdRepositoryMockWithDefaultList()
        {
            InitializeCrowdRepositoryMockWithList(this.crowdModelList);
        }

        protected void InitializeCrowdRepositoryMockWithList(List<CrowdModel> crowdModelList)
        {
            this.crowdRepositoryMock
               .Setup(repository => repository.GetCrowdCollection(It.IsAny<Action<List<CrowdModel>>>()))
               .Callback((Action<List<CrowdModel>> action) => action(crowdModelList));
            this.crowdRepositoryMock
               .Setup(repository => repository.SaveCrowdCollection(It.IsAny<Action>(), It.IsAny<List<CrowdModel>>()))
               .Callback((Action action, List<CrowdModel> cm) => action());
        }

        protected void InitializeDefaultList(bool nestCrowd = false)
        {
            CrowdModel crowdAllChars = new CrowdModel { Name = "All Characters", OriginalName = "" };
            CrowdModel crowd1 = new CrowdModel { Name = "Crowd 1", OriginalName = "" };
            CrowdModel childCrowd = new CrowdModel { Name = "Child Crowd 1.1", OriginalName = "" };
            Character character1 = new Character { Name = "Character 1.1", OriginalName = "" };
            Character character2 = new Character { Name = "Character 1.1.1 Under Child Crowd 1.1", OriginalName = "" };
            crowd1.ChildCrowdCollection = new System.Collections.ObjectModel.ObservableCollection<BaseCrowdMember>() { character1, childCrowd };
            childCrowd.ChildCrowdCollection = new System.Collections.ObjectModel.ObservableCollection<BaseCrowdMember>() { character2 };
            crowd1.ChildCrowdCollection.Add(new Character() { Name = "Character 1.2" });
            CrowdModel crowd2 = new CrowdModel { Name = "Crowd 2", OriginalName = "" };
            Character character3 = new Character { Name = "Character 2.1", OriginalName = "" };
            crowd2.ChildCrowdCollection = new System.Collections.ObjectModel.ObservableCollection<BaseCrowdMember>() { character3 };
            if (nestCrowd)
                crowd2.ChildCrowdCollection.Add(childCrowd);
            crowdAllChars.ChildCrowdCollection = new System.Collections.ObjectModel.ObservableCollection<BaseCrowdMember>() { character1, character2, character3};
            this.crowdModelList = new List<CrowdModel> { crowdAllChars, crowd1, crowd2 };
        }

        protected void DeleteTempRepositoryFile(string path = "test.data")
        {
            File.Delete(path);
        }
    }
}
