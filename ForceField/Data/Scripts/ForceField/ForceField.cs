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
        List<IMyEntity> FFBlocks = new List<IMyEntity>();

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            _ObjectBuilder = objectBuilder;
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            
            FFP = Entity as IMyBeacon;
        }

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return _ObjectBuilder;
        }

        public override void Close()
        {
            //Delete the forcefield
            foreach (var ffb in FFBlocks)
            {
                ffb.Delete();

            }
            FFBlocks.Clear();
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
                //Clear ForceFields
                foreach (var ffb in FFBlocks)
                {
                    ffb.Delete();

                }
                FFBlocks.Clear();
                
            }
        }

        public override void UpdateBeforeSimulation10()
        {
            if (isFFP)
            {
                
                //find the acceptable entites
                List<IMyEntity> Ships = AcceptableEntites();
                
                //cycles through acceptable entitiyes

                foreach(var ship in Ships)
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
            VRageMath.Base6Directions.Direction Shipdirect = FFP.GetTopMostParent().LocalMatrix.GetClosestDirection(ShipPos);

            //Direction of the projector relative to the ship will be used later
            VRageMath.Vector3D BeaconPos = FFP.GetTopMostParent().WorldAABB.Center - ship.WorldAABB.Center;
            VRageMath.Base6Directions.Direction direct = ship.LocalMatrix.GetClosestDirection(BeaconPos);
            

            //MyAPIGateway.Utilities.ShowMessage("Console God", "DirectionS = " + direct + " DirectionF = " + Shipdirect);
            //MyAPIGateway.Utilities.ShowMessage("Conosle God" , ship.WorldAABB.Max.X.ToString() + " " + ship.WorldAABB.Max.Y.ToString() + " " + ship.WorldAABB.Max.Z.ToString());
            //MyAPIGateway.Utilities.ShowMessage("Console God", ship.WorldAABB.Size.X.ToString() + " " + ship.WorldAABB.Size.Z.ToString() + " " + ship.WorldAABB.Size.Y.ToString());

            //Finds the position where the ff should spawn
            VRageMath.Vector3D FFBpos = findIntersectSphereLine(ship.GetTopMostParent().GetPosition(), FFP.GetPosition(), FFP.GetPosition(), activationRange);
            //Temp size
            VRageMath.Vector3D Size = new VRageMath.Vector3D(1,1,1);

            // bool too see if the ff should spawn
            bool createFF = true;
            
           
            //go through all of the blocks that the forcefield has spawned and see if it contains coordiates of where the ff should spawn, if it does dont spawn it
            foreach(var ffblock in FFBlocks)
            {

                VRageMath.ContainmentType cont = ffblock.WorldAABB.Contains(FFBpos);
                if(cont == VRageMath.ContainmentType.Contains || cont == VRageMath.ContainmentType.Intersects)
                {
                    createFF = false;
                }
            }
            //create the ff if  it can
            if(createFF)
                FFBlocks.Add(SpawnForceField(FFBpos, Size));
            
                       
            
            
        }


        private IMyEntity SpawnForceField(VRageMath.Vector3D pos , VRageMath.Vector3  size)
        {
            
            List<MyObjectBuilder_CubeBlock> blocks = new List<MyObjectBuilder_CubeBlock>();
            //width of the blocks
            for (int x = 0; x < 1; x++)
            {
                // height of the blocks
                for (int y = 0; y < 1; y++)
                {
                    //depth of the blocks
                    for (int z = 0; z <1; z++)
                    {
                        //Add a light armour block to the list
                        blocks.Add(new MyObjectBuilder_CubeBlock(){BuildPercent = 100,
                                                                   SubtypeName = "LargeBlockArmorBlock",
                                                                   Min = new VRageMath.Vector3I(x,y,z)
                                                                   });
                    }
                }

            }
            
            //create a grid and add the block list to it
            MyObjectBuilder_CubeGrid grid = new MyObjectBuilder_CubeGrid();
            grid.CubeBlocks.AddList(blocks);
            grid.PersistentFlags = MyPersistentEntityFlags2.InScene; 
            grid.PositionAndOrientation = new MyPositionAndOrientation(pos,FFP.GetTopMostParent().WorldMatrix.Forward,FFP.WorldMatrix.Up);
            grid.IsStatic = true;
            //spawn the ff and return it
            var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid);
            return entity;
        }



        private VRageMath.Vector3D findIntersectSphereLine(VRageMath.Vector3D point1 , VRageMath.Vector3D point2, VRageMath.Vector3D CircleC, double radius)
        {
            // we create a line from the ffprojector to the targent entity and see where it intersects a sphere around the ffprojector, that point is the point where we will spawn the ff

            //assignment for easier math
            double x0 = point1.X;
            double x1 = point2.X;
            double xc = CircleC.X;
            double y0 = point1.Y;
            double y1 = point2.Y;
            double yc = CircleC.Y;
            double z0 = point1.Z;
            double z1 = point2.Z;
            double zc = CircleC.Z;

            //Math
            double C = Math.Pow((x0 - xc),2) + Math.Pow((y0 - yc),2) + Math.Pow((z0 - zc),2)- Math.Pow(radius ,2);
            double A = Math.Pow((x0 - x1), 2) + Math.Pow((y0 - y1), 2) + Math.Pow((z0 - z1), 2); 
            double B = Math.Pow((x1 - xc), 2) + Math.Pow((y1 - yc), 2) + Math.Pow((z1 - zc), 2) - A - C - Math.Pow(radius, 2);
            
            //quadratic formula
            double t1 = (-B + Math.Sqrt(Math.Pow(B, 2) - (4 * A * C))) / (2 * A);
            double t2 = (-B - Math.Sqrt(Math.Pow(B, 2) - (4 * A * C))) / (2 * A);
            double t = 0;

            //if t > 0 && t < 1 then the line intersects the spher, we do this because there are two possible solution to the quadratic formula and we want to know the right one
            if (t1 > 0 && t1 < 1) 
                t = t1;
            else if (t2 > 0 && t2 < 1) 
                t = t2;
            //MyAPIGateway.Utilities.ShowNotification(t.ToString() + " " + t1.ToString() + " " + t2.ToString() + " " + A.ToString() + " " + B.ToString() + " " + C.ToString());
            VRageMath.Vector3D Point = new VRageMath.Vector3D();
            Point.X = x0*(1-t) + t*x1;
            Point.Y = y0*(1-t) + t*y1;
            Point.Z = z0*(1-t) + t*z1;
            return Point;
        }
    }
}
