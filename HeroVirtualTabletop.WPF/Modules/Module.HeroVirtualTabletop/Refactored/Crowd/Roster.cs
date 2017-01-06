using Framework.WPF.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeroVirtualTableTop.Crowd
{
    public class RosterImpl: Roster, NotifyPropertyChanged
    {

        public Dictionary<string, CharacterCrowd> Crowds
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public Dictionary<string, HeroVirtualTabletop.AnimatedCharacter.AnimatedCharacter> Participants
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public List<CrowdMembership> SelectedParticipants
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void AddMemberToSelection(CrowdMembership member)
        {
            throw new NotImplementedException();
        }

        public void RemoveMemberFromSelection(CrowdMembership member)
        {
            throw new NotImplementedException();
        }

        public void ClearMembersFromSelection()
        {
            throw new NotImplementedException();
        }

        public CrowdMember ActiveCharacter
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public void ActivateCharacter(CrowdMember crowdMember)
        {
            throw new NotImplementedException();
        }

        public void DeactivateCharacter(CrowdMember crowdMember)
        {
            throw new NotImplementedException();
        }

        public void AddMember(HeroVirtualTabletop.AnimatedCharacter.AnimatedCharacter member)
        {
            throw new NotImplementedException();
        }

        public void RemoveMember(HeroVirtualTabletop.AnimatedCharacter.AnimatedCharacter member)
        {
            throw new NotImplementedException();
        }

        public void SpawnToDesktop(bool completeEvent = true)
        {
            throw new NotImplementedException();
        }

        public void ClearFromDesktop(bool completeEvent = true)
        {
            throw new NotImplementedException();
        }

        public void MoveCharacterToCamera(bool completeEvent = true)
        {
            throw new NotImplementedException();
        }

        public void SaveCurrentTableTopPosition()
        {
            throw new NotImplementedException();
        }

        public void PlaceOnTableTop(Desktop.Position position = null)
        {
            throw new NotImplementedException();
        }

        public void PlaceOnTableTopUsingRelativePos()
        {
            throw new NotImplementedException();
        }

        public void Activate()
        {
            throw new NotImplementedException();
        }

        public void DeActivate()
        {
            throw new NotImplementedException();
        }
    }
}
