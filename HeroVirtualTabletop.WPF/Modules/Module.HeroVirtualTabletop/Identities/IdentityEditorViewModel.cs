using Framework.WPF.Library;
using Framework.WPF.Services.BusyService;
using Microsoft.Practices.Unity;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Library.Events;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.HeroVirtualTabletop.Identities
{
    public class IdentityEditorViewModel : BaseViewModel
    {
        #region Private Fields

        private EventAggregator eventAggregator;

        #endregion

        #region Public Properties
        #endregion

        #region Commands
        #endregion

        #region Constructor

        public IdentityEditorViewModel(IBusyService busyService, IUnityContainer container, EventAggregator eventAggregator)
            : base(busyService, container)
        {
            this.eventAggregator = eventAggregator;
            InitializeCommands();
            this.eventAggregator.GetEvent<EditIdentityEvent>().Subscribe(this.LoadIdentity);
        }

        #endregion

        #region Initialization

        private void InitializeCommands()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Methods

        private void LoadIdentity(Tuple<Identity, Character> data)
        {

        }

        #endregion

    }
}
