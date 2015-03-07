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

namespace ForceField
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon))]
    class ForceField : MyGameLogicComponent
    {

        MyObjectBuilder_EntityBase _ObjectBuilder;
        IMyBeacon FFP;
        float activationRange = 30;
        int detectionRange = 50;
        bool isFFP = false;
        List<long> Sowners = new List<long>();
        List<long> Bowners = new List<long>();
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _ObjectBuilder = objectBuilder;
            Entity.NeedsUpdate |=MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME  | MyEntityUpdateEnum.EACH_100TH_FRAME;
            
            FFP = Entity as IMyBeacon;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return _ObjectBuilder;
        }


        public override void UpdateAfterSimulation100()
        {
            isFFP = FFP.CustomName.ToLower() == "force";//checks to see if name is force making it a force field
            
            if(isFFP)
            {
                //tries to find both "big and small owners will throw exeption if it does not have owners
                try
                {
                    Sowners = (FFP.GetTopMostParent() as Sandbox.ModAPI.IMyCubeGrid).SmallOwners;
                    Bowners = (FFP.GetTopMostParent() as Sandbox.ModAPI.IMyCubeGrid).BigOwners;
                }
                catch
                { }
     
            
                
            }
        }



        public override void UpdateAfterSimulation10()
        {
            //cycles through acceptable entitiyes
            if (isFFP)
            {
                
                List<IMyEntity> Ships = AcceptableEntites();
                foreach (var ship in Ships)
                {
                    CreateFFforEntity(ship);
                }
            }

        }
        private List<IMyEntity> AcceptableEntites()
        {

            List<IMyEntity> Acceptable = new List<IMyEntity>();
            HashSet<IMyEntity> hash = new HashSet<IMyEntity>();

            //Looks for enitis that are cube grids and that do not have the same entity id as the parent of the Forcefield projector
            MyAPIGateway.Entities.GetEntities(hash, (x) => (x.GetTopMostParent().EntityId != FFP.GetTopMostParent().EntityId) && x is Sandbox.ModAPI.IMyCubeGrid || x is IMyMeteor  );
            

            
            foreach( var entity in hash )
            {
                //Checks to see if the position of the entity is smaller than the detection range but larger than the activation range
                if ((entity.GetPosition() - FFP.GetPosition()).Length() < detectionRange && (entity.GetPosition() - FFP.GetPosition()).Length() > activationRange - 3)
                {
                    //bool to see if we should add the entity
                    bool addEnt = true;
                    //checks to see if a small owner owns the ship
                    try
                    {
                        foreach (var blockowner in (entity as Sandbox.ModAPI.IMyCubeGrid).SmallOwners)
                        {
                            if (Sowners.Contains(blockowner))
                                addEnt = false;

                        }
                        //checks to see if a large owner owns the ship
                        if (addEnt)
                        {

                            foreach (var blockowner in (entity as Sandbox.ModAPI.IMyCubeGrid).BigOwners)
                            {
                                if (Bowners.Contains(blockowner))
                                    addEnt = false;

                            }
                        }

                    }
                    catch { }
                    //add the acceptable entity to the list that will be returned
                    if(addEnt)
                        Acceptable.Add(entity);
                    
                }
   
            }
            
            return Acceptable;

        }

        private void CreateFFforEntity(IMyEntity ship)
        {

            //Direction of the ship relative to the FForcefield projector will be used for later 
            VRageMath.Vector3D ShipPos = ship.WorldAABB.Center - FFP.GetTopMostParent().WorldAABB.Center;

            //Direction of the projector relative to the ship will be used later
            VRageMath.Vector3D BeaconPos = FFP.GetTopMostParent().WorldAABB.Center - ship.WorldAABB.Center;
           
            

     
            VRageMath.Vector3 impulseDirect= -2000*(BeaconPos-ShipPos);
            ship.Physics.ApplyImpulse(impulseDirect,ship.Physics.CenterOfMassWorld);

                       
            
            
        }



    }
}
