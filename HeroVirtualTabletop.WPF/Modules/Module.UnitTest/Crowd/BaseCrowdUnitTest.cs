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
            CrowdModel crowdAllChars = new CrowdModel { Name = "All Characters"};
            CrowdModel crowd1 = new CrowdModel { Name = "Crowd 1"};
            CrowdMember crowdMember1 = new CrowdMember { Name = "CrowdMember 1.1" };
            CrowdModel childCrowd = new CrowdModel { Name = "Child Crowd 1.1"};
            CrowdMember crowdMember2 = new CrowdMember { Name = "CrowdMember 1.1.1 Under Child Crowd 1.1"};
            crowd1.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>() { crowdMember1, childCrowd };
            childCrowd.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>() { crowdMember2 };
            crowd1.CrowdMemberCollection.Add(new CrowdMember() { Name = "CrowdMember 1.2" });
            CrowdModel crowd2 = new CrowdModel { Name = "Crowd 2"};
            CrowdMember crowdMember3 = new CrowdMember { Name = "CrowdMember 2.1"};
            crowd2.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>() { crowdMember3 };
            if (nestCrowd)
                crowd2.CrowdMemberCollection.Add(childCrowd);
            crowdAllChars.CrowdMemberCollection = new System.Collections.ObjectModel.ObservableCollection<ICrowdMember>() { crowdMember1, crowdMember2, crowdMember3};
            this.crowdModelList = new List<CrowdModel> { crowdAllChars, crowd1, crowd2 };
        }

        protected void DeleteTempRepositoryFile(string path = "test.data")
        {
            File.Delete(path);
        }

        protected int numberOfItemsFound = 0;
        protected void CountNumberOfCrowdMembersByName(List<ICrowdMember> collection, string name)
        {
            foreach (ICrowdMember bcm in collection)
            {
                if (bcm.Name == name)
                    numberOfItemsFound++;
                if (bcm is CrowdModel)
                {
                    CrowdModel cm = bcm as CrowdModel;
                    if (cm.CrowdMemberCollection != null && cm.CrowdMemberCollection.Count > 0)
                    {
                        CountNumberOfCrowdMembersByName(cm.CrowdMemberCollection.ToList(), name);
                    }
                }
            }
        }
    }
}
