using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Battlehub.VoxelCombat.Tests
{
    public class VoxelDataTests
    {

        [Test]
        public void CopyCtorTest()
        {
            VoxelData voxelData = new VoxelData();
            voxelData.Health = 2;
            voxelData.Dir = 1;
            voxelData.Altitude = 5;
            voxelData.Height = 15;
            voxelData.Next = new VoxelData();
            voxelData.Prev = new VoxelData();
            voxelData.Type = 1234;
            voxelData.UnitOrAssetIndex = 123;
            voxelData.Weight = 16;
            voxelData.Unit = new VoxelUnitData();
            voxelData.Unit.State = VoxelDataState.Moving;
            voxelData.Owner = 2;
            Assert.AreEqual(11, voxelData.GetType().GetFields().Length);


            VoxelData copy = new VoxelData(voxelData);
            Assert.AreEqual(copy.Health, voxelData.Health);
            Assert.AreEqual(copy.Dir, voxelData.Dir);
            Assert.AreEqual(copy.Altitude, voxelData.Altitude);
            Assert.AreEqual(copy.Height, voxelData.Height);
            Assert.AreEqual(copy.Next, voxelData.Next);
            Assert.AreEqual(copy.Prev, voxelData.Prev);
            Assert.AreEqual(copy.Type, voxelData.Type);
            Assert.AreEqual(copy.UnitOrAssetIndex, -1);
            Assert.AreEqual(copy.Weight, voxelData.Weight);
            Assert.AreNotSame(copy.Unit, voxelData.Unit);
            Assert.AreEqual(copy.Unit.State, voxelData.Unit.State);
            Assert.AreEqual(copy.Owner, voxelData.Owner);
        }
    }

}
