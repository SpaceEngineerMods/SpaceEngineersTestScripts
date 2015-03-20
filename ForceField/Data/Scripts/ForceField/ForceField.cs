using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Medieval.ObjectBuilders;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage;
using VRage.Algorithms;

using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;


namespace ForceField
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
    class ForceField : MyGameLogicComponent
    {

        private MyObjectBuilder_EntityBase _objectBuilder;
        private Sandbox.ModAPI.IMyCubeGrid _ffp;
        private double _activationRange;
        private bool _isFfp;

        private List<Sandbox.ModAPI.IMySlimBlock> _ffProjectors;
        private List<Sandbox.ModAPI.IMySlimBlock> _ffPowerBlocks;
        private List<Sandbox.ModAPI.IMySlimBlock> _ffAmplifierBlocks;
        private List<Sandbox.ModAPI.IMySlimBlock> _ffReactorBlocks;
        private List<Sandbox.ModAPI.IMySlimBlock> _ffProviders;
        private double _fFpowerMult = 0.5;

        private int _timer;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;

            _ffp = Entity as Sandbox.ModAPI.IMyCubeGrid;

            _ffPowerBlocks = new List<IMySlimBlock>();
            _ffAmplifierBlocks = new List<IMySlimBlock>();
            _ffProjectors = new List<IMySlimBlock>();
            _ffReactorBlocks = new List<IMySlimBlock>();
            _ffProviders = new List<IMySlimBlock>();
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return _objectBuilder;
            
            
        }


        public override void UpdateAfterSimulation100()
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            _ffPowerBlocks.Clear();
            _ffAmplifierBlocks.Clear();
            _ffProjectors.Clear();
            _ffReactorBlocks.Clear();

            CheckFfblocks();

            _isFfp = _ffProjectors.Any();

            if (!_isFfp) return;
            //sets power 


            //sets amplification of power

            foreach (var amplifier in _ffAmplifierBlocks)
            {
                if (amplifier.FatBlock.DisplayNameText.ToLower().Contains("main"))
                {
                    try
                    {
                        _fFpowerMult = ((amplifier.FatBlock as IMyRadioAntenna).Radius / 100) * _ffAmplifierBlocks.Count();

                    }
                    catch
                    {
                        //ignored
                    }
                }
                else
                {
                    _fFpowerMult = 0.5;
                }

            }

            foreach (var projector in _ffProjectors)
            {
                _activationRange = (projector.FatBlock as IMyRadioAntenna).Radius;
            }



            //MyAPIGateway.Utilities.ShowNotification(_timer.ToString());


        }



        public override void UpdateBeforeSimulation10()
        {
            if (!_isFfp) return;
            _timer++;


            if (_timer > 6)
            {
                ReportPower();
                _timer = 0;
            }

            //cycles through acceptable entitiyes
            List<IMyEntity> ships = AcceptableEntites();
            foreach (var ship in ships)
            {
                CreateFFforEntity(ship);
            }
            
        }

        private List<IMyEntity> AcceptableEntites()
        {

            List<IMyEntity> acceptable = new List<IMyEntity>();
            HashSet<IMyEntity> hash = new HashSet<IMyEntity>();

            //Looks for enitis that are cube grids and that do not have the same entity id as the parent of the Forcefield projector
            MyAPIGateway.Entities.GetEntities(hash, x => x != null);
            //x.GetTopMostParent().EntityId != FFP.GetTopMostParent().EntityId) && x is Sandbox.ModAPI.IMyCubeGrid || x is IMyMeteor || 


            foreach (var entity in hash)
            {
                //Checks to see if the position of the entity is smaller than the detection range but larger than the activation range
                if ((entity.GetPosition() - _ffp.GetPosition()).Length() > _activationRange) continue;
                //bool to see if we should add the entity
                bool addEnt = true;
                //checks to see if a small owner owns the ship
                try
                {
                    var myCubeGrid = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (myCubeGrid != null)
                        foreach (var player in myCubeGrid.SmallOwners)
                        {
                            foreach (var force in _ffProjectors)
                            {
                                
                                
                                addEnt = !(force.FatBlock as Sandbox.ModAPI.Ingame.IMyTerminalBlock).HasPlayerAccess(player);
                            }
                        }
                }
                catch
                {
                    // ignored
                }
                //add the acceptable entity to the list that will be returned
                if (addEnt)
                    acceptable.Add(entity);
            }

            return acceptable;

        }

        private void CreateFFforEntity(IMyEntity ship)
        {

            try
            {
                VRageMath.Vector3D shipAbsPos = ship.WorldAABB.Center;
                VRageMath.Vector3D ffpAbsPos = _ffp.WorldAABB.Center;

                double range = (shipAbsPos - ffpAbsPos).Length();


                double percent = Math.Abs(((range) / (_activationRange)) - 1);

                //MyAPIGateway.Utilities.ShowNotification(ship.Physics.Mass.ToString());
                //creates the actual force field by appling the force
                double amount = (percent * _fFpowerMult * (ship.Physics.Mass / 100)) / 1000;
                //MyAPIGateway.Utilities.ShowMessage("Console God ",((MyFixedPoint)amount).ToString());

                if (ReducePower(amount))
                {
                    VRageMath.Vector3 impulseDirect = (ship.Physics.Mass / 3) * (shipAbsPos - ffpAbsPos) * percent * _fFpowerMult;
                    ship.Physics.ApplyImpulse(impulseDirect, ship.Physics.CenterOfMassWorld);
                    _ffp.GetTopMostParent().Physics.ApplyImpulse(-impulseDirect, _ffp.GetTopMostParent().Physics.CenterOfMassWorld);
                }

            }
            catch
            {
                // ignored
            }


        }

        private void CheckFfblocks()
        {



            try
            {
                _ffp.GetBlocks(_ffProjectors, b => b.FatBlock is IMyRadioAntenna && b.FatBlock.DisplayNameText.ToLower().Contains("force") && b.FatBlock.IsWorking);

                _ffp.GetBlocks(_ffAmplifierBlocks, b => b.FatBlock is IMyRadioAntenna && b.FatBlock.DisplayNameText.ToLower().Contains("amp") && b.FatBlock.IsWorking);
                //MyAPIGateway.Utilities.ShowMessage("consolegod","test");
                _ffp.GetBlocks(_ffProviders, b => b.FatBlock is IMyCargoContainer && b.FatBlock.DisplayNameText.ToLower().Contains("provider") && b.FatBlock.IsWorking);
            }
            catch
            {
                //ignored
            }

        }

        public void ReportPower()
        {
            double KGD = 0;
            if (VRageMath.ContainmentType.Contains != _ffp.WorldAABB.Contains(MyAPIGateway.Session.Player.GetPosition()))
                return;


            if (!(new List<IMySlimBlock>(_ffProjectors.Where(
                x => (x.FatBlock as IMyRadioAntenna).HasPlayerAccess(MyAPIGateway.Session.Player.PlayerID))).Any()))
                return;

            

            foreach (var provider in _ffProviders)
            {

                Sandbox.ModAPI.IMyInventory inv = (Sandbox.ModAPI.IMyInventory)(provider.FatBlock as Sandbox.ModAPI.Interfaces.IMyInventoryOwner).GetInventory(0);


                foreach (var inventoryItem in inv.GetItems())
                {

                    if (inventoryItem.Content.SubtypeName != "Construction")
                    {

                        KGD += ((double)inventoryItem.Amount) * _fFpowerMult;

                    }
                }
            }


            //MyAPIGateway.Utilities.ShowMessage("console GOd " , percentPower.ToString());
            if (KGD == 0)
            {
                MyAPIGateway.Utilities.ShowNotification("Shields Down", 2000, MyFontEnum.Red);
            }



        }

        private bool ReducePower(double amount)
        {
            
            foreach (var provider in _ffProviders)
            {

                Sandbox.ModAPI.IMyInventory inv = (Sandbox.ModAPI.IMyInventory)(provider.FatBlock as Sandbox.ModAPI.Interfaces.IMyInventoryOwner).GetInventory(0);


                foreach (var inventoryItem in inv.GetItems())
                {

                    if (inventoryItem.Content.SubtypeName != "Construction")
                    {
                        if ((MyFixedPoint)amount == 0)
                        {
                            return true;
                        }
                        if (inventoryItem.Amount >= (MyFixedPoint)amount)
                        {


                            inv.RemoveItems(inventoryItem.ItemId, (MyFixedPoint)amount);

                            return true;
                        }

                        amount -= (Double)inventoryItem.Amount;
                        /*
                        MyAPIGateway.Utilities.ShowMessage("Console God",
                               test.Amount + " " + (MyFixedPoint)amount + " " +
                               (test.Amount - (MyFixedPoint)amount)); 
                         */
                        inv.RemoveItems(inventoryItem.ItemId, inventoryItem.Amount);
                    }
                }
            }
            return false;
        }
    }
}
