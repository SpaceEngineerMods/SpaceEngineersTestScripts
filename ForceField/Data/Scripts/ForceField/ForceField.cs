//Todo:
//Calculate size of forcefield
//Stop missiles
//create custom Models
//create a force field percentage


using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRage.Algorithms;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
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
        private double _fFpowerMult = 0.5;
        private double _fFpower;
        private double _fFMaxpower;
        private double _ffRegenRate;

        private int _timer;
        private bool _disabled;
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;
            Entity.NeedsUpdate |=MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME  | MyEntityUpdateEnum.EACH_100TH_FRAME;
            
            _ffp = Entity as Sandbox.ModAPI.IMyCubeGrid;

            _ffPowerBlocks = new List<IMySlimBlock>();
            _ffAmplifierBlocks = new List<IMySlimBlock>();
            _ffProjectors = new List<IMySlimBlock>();
            _ffReactorBlocks = new List<IMySlimBlock>();
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return _objectBuilder;
        }


        public override void UpdateAfterSimulation100()
        {
            _ffPowerBlocks.Clear();
            _ffAmplifierBlocks.Clear();
            _ffProjectors.Clear();
            _ffReactorBlocks.Clear();

            CheckFfblocks();

            _isFfp = _ffProjectors.Any();

            if (!_isFfp) return;
           //sets power 
            
            _ffRegenRate = 0;
            foreach (var reactor in _ffReactorBlocks)
            {
                string reactorInfo = (reactor.FatBlock as IMyReactor).DetailedInfo;

                string num = new string(reactorInfo.ToCharArray().Where(c => Char.IsDigit(c) || c.Equals('.') || c ==' ').ToArray()).Trim();

                num = num.Remove(num.IndexOf(' '));
                _ffRegenRate += Convert.ToDouble(num);
                //MyAPIGateway.Utilities.ShowNotification(_ffRegenRate.ToString());

            }

            _fFMaxpower = _ffPowerBlocks.Count * 1000;
            if (_fFpower < _fFMaxpower)
            {
                _fFpower += _ffRegenRate;
            }
            else
            {
                _fFpower = _fFMaxpower;
            }

            //sets amplification of power
            
            foreach (var amplifier in _ffAmplifierBlocks)
            {
                if(amplifier.FatBlock.DisplayNameText.ToLower().Contains("main"))
                {
                    try
                    {
                        _fFpowerMult = ((amplifier.FatBlock as IMyRadioAntenna).Radius/100)*_ffAmplifierBlocks.Count();

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

            if (_disabled)
                reportPower();
        }



        public override void UpdateBeforeSimulation10()
        {
            if(!_isFfp)return;
            _timer++;


            if (_timer > 5 && !_disabled)
            {
                reportPower();
                _timer = 0;
            }
            else if (_timer > 50 && _disabled)
            {
                reportPower();
                _timer = 0;
                _disabled = false;
            }
            //cycles through acceptable entitiyes
            if ( _fFpower <= 0 || _disabled) return;
            
            
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
            MyAPIGateway.Entities.GetEntities(hash, x => x != null  );
            //x.GetTopMostParent().EntityId != FFP.GetTopMostParent().EntityId) && x is Sandbox.ModAPI.IMyCubeGrid || x is IMyMeteor || 

            
            foreach( var entity in hash )
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
                if(addEnt)
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
                
                double range = (shipAbsPos-ffpAbsPos).Length();
                
                
                double percent = Math.Abs(((range)/ (_activationRange)) - 1) ;
                
                //MyAPIGateway.Utilities.ShowNotification(ship.Physics.Mass.ToString());
                //creates the actual force field by appling the force
                _fFpower -= percent * (ship.Physics.Mass / 100) * _fFpowerMult;
                

                if (_fFpower > 0)
                {
                    VRageMath.Vector3 impulseDirect = (ship.Physics.Mass/3)*(shipAbsPos - ffpAbsPos)*percent*
                                                      _fFpowerMult;
                    ship.Physics.ApplyImpulse(impulseDirect, ship.Physics.CenterOfMassWorld);
                    _ffp.GetTopMostParent()
                        .Physics.ApplyImpulse(-impulseDirect, _ffp.GetTopMostParent().Physics.CenterOfMassWorld);
                }
                else
                {
                    _disabled = true;
                    
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
                _ffp.GetBlocks(_ffProjectors, b => b.FatBlock is IMyRadioAntenna && b.FatBlock.DisplayNameText.ToLower().Contains("force")  && b.FatBlock.IsWorking);
                
                _ffp.GetBlocks(_ffPowerBlocks, b => b.FatBlock is IMyBatteryBlock && b.FatBlock.DisplayNameText.ToLower().Contains("power")&& b.FatBlock.IsWorking);

                _ffp.GetBlocks(_ffAmplifierBlocks, b => b.FatBlock is IMyRadioAntenna && b.FatBlock.DisplayNameText.ToLower().Contains("amp") && b.FatBlock.IsWorking);
                //MyAPIGateway.Utilities.ShowMessage("consolegod","test");
                _ffp.GetBlocks(_ffReactorBlocks, b => b.FatBlock is IMyReactor && b.FatBlock.DisplayNameText.ToLower().Contains("power") && b.FatBlock.IsWorking);
            }
            catch
            {
                //ignored
            }
            
        }

        public void reportPower()
        {
            double percentPower = _fFpower/_fFMaxpower;
            if (VRageMath.ContainmentType.Contains != _ffp.WorldAABB.Contains(MyAPIGateway.Session.Player.GetPosition()))
                return;

            
            if(!(new List<IMySlimBlock>(_ffProjectors.Where(
                x => (x.FatBlock as IMyRadioAntenna).HasPlayerAccess(MyAPIGateway.Session.Player.PlayerID))).Any()))
                return;

            //MyAPIGateway.Utilities.ShowMessage("console GOd " , percentPower.ToString());

            if (_disabled)
            {
                MyAPIGateway.Utilities.ShowNotification("Shields Broken!!", 2000, MyFontEnum.Red);
            }
            else if (percentPower < .25)
            {
                MyAPIGateway.Utilities.ShowNotification("Shield Power Below 25%", 2000, MyFontEnum.Red);
            }
            else if (percentPower < .50)
            {
                MyAPIGateway.Utilities.ShowNotification("Shield Power Below 50%");
            }
            else if (percentPower < .75)
            {
               MyAPIGateway.Utilities.ShowNotification("Shield Power Below 75%",2000,MyFontEnum.Green);
            }
           
            
      
        }
    }
}
