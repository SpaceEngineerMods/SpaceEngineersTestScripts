//Todo:
//Calculate size of forcefield
//Stop missiles
//create custom Models
//create a force field percentage


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;


namespace ForceField
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
    class ForceField : MyGameLogicComponent
    {

        MyObjectBuilder_EntityBase _objectBuilder;
        Sandbox.ModAPI.IMyCubeGrid _ffp;
        double _activationRange = 80;
        double _detectionRange;
        bool _isFfp;
        HashSet<IMyFaction> _bfaction = new HashSet<IMyFaction>();
        private List<Sandbox.ModAPI.IMySlimBlock> _ffProjectors;
        
        double _fFpowerMult = 0.5;
        double _fFpower = 100;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _objectBuilder = objectBuilder;
            Entity.NeedsUpdate |=MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME  | MyEntityUpdateEnum.EACH_100TH_FRAME;
            
            _ffp = Entity as Sandbox.ModAPI.IMyCubeGrid;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return _objectBuilder;
        }


        public override void UpdateAfterSimulation100()
        {
            _ffProjectors = CheckFfp();
            _isFfp = _ffProjectors.Any();

           

            if (!_isFfp) return;
            //tries to find both "big and small owners will throw exeption if it does not have owners
            MyAPIGateway.Utilities.ShowMessage("ConsoleGod", _ffProjectors.Count.ToString());
            try
            {
                List<long> sowners = (_ffp.GetTopMostParent() as Sandbox.ModAPI.IMyCubeGrid).SmallOwners;
                IMyFactionCollection factions = MyAPIGateway.Session.Factions;
                foreach(var play in sowners)
                {
                    _bfaction.Add(factions.TryGetPlayerFaction(play));
                }
            }

            catch
            {
                // ignored
            }

            _detectionRange =(_activationRange * 3);
        }



        public override void UpdateBeforeSimulation10()
        {
            
            
            //cycles through acceptable entitiyes
            if (!_isFfp) return;

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
                if (!((entity.GetPosition() - _ffp.GetPosition()).Length() < _detectionRange) ||
                    !((entity.GetPosition() - _ffp.GetPosition()).Length() > _activationRange - 3)) continue;
                //bool to see if we should add the entity
                bool addEnt = true;
                //checks to see if a small owner owns the ship
                try
                {
                    var myCubeGrid = entity as Sandbox.ModAPI.IMyCubeGrid;
                    if (myCubeGrid != null)
                        foreach (var player in myCubeGrid.SmallOwners)
                        {


                            foreach (var fac in _bfaction)
                            {
                                addEnt = fac.IsMember(player);
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
                double range = Math.Sqrt(Math.Pow(ffpAbsPos.X - shipAbsPos.X ,2) + Math.Pow(ffpAbsPos.Y - shipAbsPos.Y ,2) + Math.Pow(ffpAbsPos.Z -shipAbsPos.Z ,2));
                
                
                double percent = Math.Abs(((range)/ (_detectionRange)) - 1) ;
                
                //MyAPIGateway.Utilities.ShowNotification(percent.ToString());

                VRageMath.Vector3 impulseDirect = (ship.Physics.Mass/3) * (shipAbsPos - ffpAbsPos) * percent * _fFpowerMult ;
                ship.Physics.ApplyImpulse(impulseDirect, ship.Physics.CenterOfMassWorld);
                _ffp.GetTopMostParent().Physics.ApplyImpulse(-impulseDirect, _ffp.GetTopMostParent().Physics.CenterOfMassWorld);
            }
            catch
            {
                // ignored
            }
        }

        private List<Sandbox.ModAPI.IMySlimBlock> CheckFfp()
        {

            List<Sandbox.ModAPI.IMySlimBlock> blockhash = new List<Sandbox.ModAPI.IMySlimBlock>();

            try
            {
                _ffp.GetBlocks(blockhash, b => b.FatBlock is IMyRadioAntenna && b.FatBlock.DisplayNameText.ToLower().Contains("force"));
            }
            catch
            {
                //ignored
            }
            return blockhash;
        }

    }
}
