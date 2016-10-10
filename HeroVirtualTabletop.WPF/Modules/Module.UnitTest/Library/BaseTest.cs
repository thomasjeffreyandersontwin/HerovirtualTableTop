using Framework.WPF.Services.BusyService;
using Framework.WPF.Services.MessageBoxService;
using Framework.WPF.Services.PopupService;
using Microsoft.Practices.Unity;
using Moq;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.UnitTest
{
    public class BaseTest
    {
        protected Mock<IBusyService> busyServiceMock = new Mock<IBusyService>();
        protected Mock<IPopupService> popupServiceMock = new Mock<IPopupService>();
        protected Mock<EventAggregator> eventAggregatorMock = new Mock<EventAggregator>();
        protected Mock<IMessageBoxService> messageBoxServiceMock = new Mock<IMessageBoxService>();
        protected Mock<IUnityContainer> unityContainerMock = new Mock<IUnityContainer>();
    }
}
