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

namespace Teleporter
{
<<<<<<< HEAD
    //master
=======
    //testing
>>>>>>> Testing
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door))]
    public class Teleporter : MyGameLogicComponent
    {
        TeleportationManager man = new TeleportationManager();

        IMyDoor entrance_g = null;
        public IMyDoor exit_g = null;
        bool isportal = false;
        bool isactive = true;
        bool WasUsed = false;
        private int m_timer = 0;
        public override void Close()
        {
            entrance_g.DoorStateChanged -= DoorSateChanged;
        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            entrance_g = this.Entity as IMyDoor;
            entrance_g.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
           // MyAPIGateway.Utilities.ShowNotification("Is Running", 10000, MyFontEnum.Red);
            entrance_g.DoorStateChanged += DoorSateChanged;
        }

        void DoorSateChanged(bool obj)
        {
            if (!Entity.NeedsUpdate.HasFlag(MyEntityUpdateEnum.EACH_10TH_FRAME))
            {
                Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            }

            
           
            VRageRender.MyRenderMessagePlayVideo cool = new VRageRender.MyRenderMessagePlayVideo();
            cool.VideoFile = "Big_Gun_01";
            cool.Volume = 1.0f;

            //LogMessage(myDoor.LocalAABB.ToString(), false);
            //LogMessage(myDoor.LocalMatrix.ToString(), false);
        }

        public override void UpdateAfterSimulation()
        {
            
        }
        public override void UpdateAfterSimulation10()
        {


        }





        public override void UpdateAfterSimulation100()
        {
            if (WasUsed && ++m_timer % 7 == 0)
            {
                WasUsed = false;
                m_timer = 0;
                isactive = true;
                MyAPIGateway.Utilities.ShowNotification("Cooldown Finished", 2500);
                man.ActivatePortal(entrance_g);
                man.ActivatePortal(exit_g);
                man.GetInactivePortals();
                
            }

            if (isactive && WasUsed)
            {
                isactive = false;
                MyAPIGateway.Utilities.ShowNotification("Cooldown starting", 2500);
            }  




        }







        public override void UpdateBeforeSimulation()
        {

        }






        public override void UpdateBeforeSimulation10()
        {

            if (entrance_g == null)
                return;

            if (!entrance_g.IsFunctional || !entrance_g.IsWorking)
                return;

            string myname = entrance_g.CustomName;

            isportal = myname != null && myname.Contains("Portal");
            

           

            if (isportal)
            {

                //  MyAPIGateway.Utilities.ShowNotification("I am a Portal", 1000, MyFontEnum.Red);
            
               

                    
                exit_g = GetNearestGateOnDifferentGrid(entrance_g);
                //if (exit_g != null)
                   // MyAPIGateway.Utilities.ShowNotification("Found Portal", 2500);
               //else
                    //MyAPIGateway.Utilities.ShowNotification("No portals found", 2500);

                //Sandbox.ModAPI.MyAPIGateway.Utilities.ShowNotification("UpdateBeforeSimulation10", 100);

                var player = MyAPIGateway.Session.Player.PlayerCharacter;

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity != null)
                    player = MyAPIGateway.Session.Player.Controller.ControlledEntity;

                //VRageMath.Vector3[] corners = m_gate.WorldAABB.GetCorners();
                //VRageMath.BoundingBox box = m_gate.WorldAABB;

                //foreach (var corner in corners)
                    //Sandbox.ModAPI.MyAPIGateway.Utilities.ShowNotification(String.Format("X={0}, Y={1}, Z={2}", corner.X, corner.Y, corner.Z, 5000));

                // Check if player position is at event horizon
                // This distance is based on local origin point for the model and player/cockpit
                // A more accurate method would be to constrain the bounds to the plane parallel
                // to the gate and through the center, and within the bounding box of the model.

                float distance = (player.Entity.GetPosition() - entrance_g.GetPosition()).Length();
                //MyAPIGateway.Utilities.ShowNotification("Distance from Portal = " + distance, 2500);
                
                if (distance < 1.8f &&  isactive && exit_g != null)
                {

                    if (man.Teleportplayer(entrance_g, exit_g, player))
                    {
                        WasUsed = true;
                        isactive = false;
                    }
                    

                    
                   
                }
               
                
            }
        }


        private List<Sandbox.ModAPI.IMySlimBlock> GetGateList()
        {
            HashSet<IMyEntity> hash = new HashSet<IMyEntity>();
            List<Sandbox.ModAPI.IMySlimBlock> gateList = new List<Sandbox.ModAPI.IMySlimBlock>();

            Sandbox.ModAPI.MyAPIGateway.Entities.GetEntities(hash, (x) => x is Sandbox.ModAPI.IMyCubeGrid);
            

            foreach (var entity in hash)
            {
                List<Sandbox.ModAPI.IMySlimBlock> blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                Sandbox.ModAPI.IMyCubeGrid grid = entity as Sandbox.ModAPI.IMyCubeGrid;
                
                try
                {
                    grid.GetBlocks(blocks, (x) => x.FatBlock is IMyDoor && 
                                                  (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Portal") && 
                                                  man.isActive(x.FatBlock));
                    
                }
                catch
                {
                    MyAPIGateway.Utilities.ShowNotification("Error When trying to find Portals", 250);
                }


                foreach (var block in blocks)
                {
                    try
                    {

                        String name = (block.FatBlock as IMyTerminalBlock).CustomName;
                        gateList.Add(block);
                        //MyAPIGateway.Utilities.ShowNotification("Added Door,  Pos = " + block.FatBlock.Position.ToString() +
                         // " Name = " + name, 250);
                    }
                    catch
                    {
                        MyAPIGateway.Utilities.ShowNotification("Error", 250);
                    }
                }
                    
            }
             
            
            return gateList;
        }
        
        private IMyDoor GetNearestGateOnDifferentGrid(IMyDoor sourceGate)
        {
            List<Sandbox.ModAPI.IMySlimBlock> gateList = GetGateList();
            double distance = 0.0d;
            IMyDoor nearest = null;
            
            //MyAPIGateway.Utilities.ShowNotification("Searching for portals", 250);
            
            foreach (var gate in gateList)
            {
                

                // Skip disabled, or destroyed gates
                // Skip if on same grid as source gate
                if (gate.IsDestroyed || !gate.FatBlock.IsFunctional ||
                    gate.FatBlock.GetTopMostParent().EntityId == sourceGate.GetTopMostParent().EntityId)
                    continue;

                // THen find the closest.
                if (distance == 0.0d || (entrance_g.GetPosition() - gate.FatBlock.GetPosition()).Length() < distance)
                {
                    nearest = gate.FatBlock as IMyDoor;
                    distance = (entrance_g.GetPosition() - gate.FatBlock.GetPosition()).Length();
                }
            }
            //if(nearest != null)
                //MyAPIGateway.Utilities.ShowNotification("Nearest Gate = " + nearest.CustomName, 250);
            return nearest;
            
        }

        

    }
    


}