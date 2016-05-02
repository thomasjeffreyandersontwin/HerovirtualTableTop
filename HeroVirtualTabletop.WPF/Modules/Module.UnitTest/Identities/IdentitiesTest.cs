using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module.HeroVirtualTabletop.Characters;
using Module.HeroVirtualTabletop.Identities;
using Moq;
using Module.HeroVirtualTabletop.Library.Enumerations;
using System.IO;
using Module.Shared;

namespace Module.UnitTest.Identities
{
    [TestClass]
    public class IdentitiesTest
    {
        Character TestCharacter = new Mock<Character>().Object;

        [TestMethod]
        public void IdentitiesCollection_EmptyCollectionDefaultIsBaseIdentity()
        {
            Identity actual = TestCharacter.ActiveIdentity;
            Identity valid = new Identity("model_Statesman", IdentityType.Model, "Base");
            Assert.AreEqual(valid, actual);
        }

        [TestMethod]
        public void DefaultIdentity_CharacterWithNameThatMatchExistingCostumeGetsProperIdentity()
        {
            string costumeFile = Path.Combine(Constants.GAME_COSTUMES_FOLDERNAME, "Spyder" + Constants.GAME_COSTUMES_EXT);
            Directory.CreateDirectory(Constants.GAME_COSTUMES_FOLDERNAME);
            FileStream f = File.Create(costumeFile);
            f.Dispose();
            
            Character c = new Character("Spyder");
            Identity actual = c.ActiveIdentity;
            Identity valid = new Identity("Spyder", IdentityType.Costume, "Spyder");
            Assert.AreEqual(valid, actual);

            Directory.Delete(Constants.GAME_COSTUMES_FOLDERNAME, true);
        }
    }
}
