using Framework.WPF.Library;
using Module.HeroVirtualTabletop.Crowds;
using Module.HeroVirtualTabletop.Library.ProcessCommunicator;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Module.UnitTest
{
    public class BaseCrowdTest : BaseTest
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

        protected void InitializeMessageBoxService(MessageBoxResult messageBoxResult)
        {
            this.messageBoxServiceMock
                .Setup(messageboxService => messageboxService.ShowDialog(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageBoxButton>(), It.IsAny<MessageBoxImage>()))
                .Returns(messageBoxResult);
        }

        protected void InitializeDefaultList(bool nestCrowd = false)
        {
            CrowdModel crowdAllChars = new CrowdModel { Name = "All Characters" };
            CrowdModel crowd1 = new CrowdModel { Name = "Gotham City" };
            CrowdMemberModel crowdMember1 = new CrowdMemberModel { Name = "Batman" };
            crowd1.SavedPositions = new Dictionary<string, IMemoryElementPosition>();
            CrowdModel childCrowd = new CrowdModel { Name = "The Narrows"};
            CrowdMemberModel crowdMember2 = new CrowdMemberModel { Name = "Scarecrow"};
            crowd1.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name) { crowdMember1, childCrowd };
            childCrowd.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name) { crowdMember2 };
            CrowdMemberModel crowdMember4 = new CrowdMemberModel() { Name = "Robin" };
            crowd1.CrowdMemberCollection.Add(crowdMember4);
            CrowdModel crowd2 = new CrowdModel { Name = "League of Shadows" };
            CrowdMemberModel crowdMember3 = new CrowdMemberModel { Name = "Ra'as Al Ghul"};
            crowd2.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name) { crowdMember3 };
            if (nestCrowd)
                crowd2.CrowdMemberCollection.Add(childCrowd);
            crowdAllChars.CrowdMemberCollection = new SortableObservableCollection<ICrowdMemberModel, string>(x => x.Name) { crowdMember1, crowdMember2, crowdMember3, crowdMember4};
            this.crowdModelList = new List<CrowdModel> { crowdAllChars, crowd1, crowd2, childCrowd };
        }

        protected IMemoryElementPosition GetRandomPosition()
        {
            Random rand = new Random();
            Mock<IMemoryElementPosition> position = new Mock<IMemoryElementPosition>();
            Random random = new Random(rand.Next());
            position.Setup(p => p.X).Returns(random.Next(-1000, 1000));
            position.Setup(p => p.Y).Returns(random.Next(-1000, 1000));
            position.Setup(p => p.Z).Returns(random.Next(-1000, 1000));

            Mock<IMemoryElementPosition> clonedPosition = new Mock<IMemoryElementPosition>();
            clonedPosition.Setup(cp => cp.X).Returns(position.Object.X);
            clonedPosition.Setup(cp => cp.Y).Returns(position.Object.Y);
            clonedPosition.Setup(cp => cp.Z).Returns(position.Object.Z);
            position.Setup(p => p.Clone(It.IsAny<bool>(), It.IsAny<uint>())).Returns(clonedPosition.Object);

            return position.Object;
        }

        protected void DeleteTempRepositoryFile(string path = "test.data")
        {
            File.Delete(path);
        }

        protected int numberOfItemsFound = 0;
        protected void CountNumberOfCrowdMembersByName(List<ICrowdMemberModel> collection, string name)
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

        protected List<ICrowdMemberModel> GetFlattenedMemberList(List<ICrowdMemberModel> list)
        {
            List<ICrowdMemberModel> _list = new List<ICrowdMemberModel>();
            foreach (ICrowdMemberModel cm in list)
            {
                if (cm is CrowdModel)
                {
                    CrowdModel crm = (cm as CrowdModel);
                    if (crm.CrowdMemberCollection != null && crm.CrowdMemberCollection.Count > 0)
                        _list.AddRange(GetFlattenedMemberList(crm.CrowdMemberCollection.ToList()));
                }
                _list.Add(cm);
            }
            return _list;
        }

    }
}
