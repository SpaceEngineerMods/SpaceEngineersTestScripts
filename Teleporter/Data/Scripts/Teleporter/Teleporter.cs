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

//Basic imports



namespace Teleporter//teleporter namespace
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door))]//Type of door, applies to doors

    public class Teleporter : MyGameLogicComponent//class teleporter, calls from game logic, further describes what a teleporter is
    {
        TeleportationManager man = new TeleportationManager();//creates new teleporter manager, to manage portals

        IMyDoor entrance_g = null;//public, portal entrance

        public IMyDoor exit_g = null;//public, which is the exit

        bool isportal = false;//bool used in determining whether it was a portal

        bool isactive = true;//bool determining whether a portal works or not

        bool WasUsed = false;//bool determining whether a portal was just used

        private int m_timer = 0;//timer

        
        public override void Close()//Door state Changed
        {
            entrance_g.DoorStateChanged -= DoorSateChanged;//Closes the door
        }
        public override void Init(MyObjectBuilder_EntityBase objectBuilder)//initializes object, overwrites original object code making it an entrance
        {
            entrance_g = this.Entity as IMyDoor;//says this entity is an IMyDoor Called entrance_g

            entrance_g.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;//Door state updated every 10th/ 100th frame

            entrance_g.DoorStateChanged += DoorSateChanged;//Opens door

        }

        void DoorSateChanged(bool obj)//sets up when and how to detect changes
        {
            if (!Entity.NeedsUpdate.HasFlag(MyEntityUpdateEnum.EACH_10TH_FRAME))//if it does not have a flag every ten frames
            {
                Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;//sets needs update to 10 or 100 frames
            }
        }







        public override void UpdateAfterSimulation100()//updates every 100 frames
        {
            if (WasUsed && ++m_timer % 7 == 0)//after 700 frames from whenever wasused was turned to true
            {
                WasUsed = false;//set wasused to false

                m_timer = 0;//set m_timer to 0
                
                isactive = true;//set isactive to true

                MyAPIGateway.Utilities.ShowNotification("Cooldown Finished");//hud notification

                man.ActivatePortal(entrance_g);//activates portal entrance_g

                man.ActivatePortal(exit_g);//activates portal exit_g
                
                
            }

            if (isactive && WasUsed)//beginning of cooldown
            {
                MyAPIGateway.Utilities.ShowNotification("Cooldown starting");//hud notification
            } 
        }






        //This is the actual check for teleportation
        public override void UpdateBeforeSimulation10()//rewriting the door update stuff, activating every 10 frames
        {

            if (entrance_g == null)//iz

                return;//do nothing

            if (!entrance_g.IsFunctional || !entrance_g.IsWorking) //if entrance g is not functional or working

                return;//do nothing

            string myname = entrance_g.CustomName;//create string myname, name of door

            isportal = myname != null && myname.Contains("Portal"); //if my name contains portal, isportal = true

            if (isportal && isactive)//if isportal is true
            {
                //MyAPIGateway.Utilities.ShowNotification("Is portal");    
                exit_g = GetNearestGateOnDifferentGrid(entrance_g);//get nearest other gate,on a different grid from entrance_g

                /*if (exit_g == null)// if there is not a portal that this is connected to
                {
                    entrance_g; do not know how to call up open and close settings for door
                }
                else
                {
                    
                }*/
                var player = MyAPIGateway.Session.Player.Client as IMyControllableEntity;//creates variable player, inherets playercharacter position

                if (MyAPIGateway.Session.Player.Controller.ControlledEntity != null)//Makes sure Controlled Entity is a player

                    player = MyAPIGateway.Session.Player.Controller.ControlledEntity;//player is a controlled entity

                float distance = (player.Entity.GetPosition() - entrance_g.GetPosition()).Length();//distance is a three part vector, is equal to the distance from the player to the portal node
                //MyAPIGateway.Utilities.ShowNotification("Distance = " + distance);
                //MyAPIGateway.Utilities.ShowNotification((exit_g == null).ToString());
                if (distance < 1.8f &&  isactive && exit_g != null)//if the distance is less than 1.8f(whatever that is) and both the entrance and exit exist and are operational
                {
                    //MyAPIGateway.Utilities.ShowNotification("Will Teleport");
                    if (man.Teleportplayer(entrance_g, exit_g, player))//no idea what this says
                    {
                        //test code plz ignore

                        
                        
                        try
                        {
                            //do not delete
                            /*
                            var blueprints = MyDefinitionManager.Static.GetAllDefinitions();

                            var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage("definitions.txt",typeof(String));
                            foreach(var ship in blueprints)
                            {
                                writer.Write(ship.Id.ToString()+ "\n");
                            }
                            */
                            
                            // Makes a quternion object that controls the doors orientation 
                            Sandbox.Common.ObjectBuilders.VRageData.SerializableQuaternion d_Orient = new Sandbox.Common.ObjectBuilders.VRageData.SerializableQuaternion(0, 0,0 , 0);//x,y,z,w
                            
                            VRageMath.Quaternion quad = new VRageMath.Quaternion(0, -1, 0, 0);//base block orientato
                            Sandbox.Common.ObjectBuilders.VRageData.SerializableBlockOrientation b_Orient = new Sandbox.Common.ObjectBuilders.VRageData.SerializableBlockOrientation(VRageMath.Base6Directions.Direction.Forward,VRageMath.Base6Directions.Direction.Up);
                            
                            /*
                            //Creates a door object builder and sets its build percent to 100 percent and its orientation to orient.
                            MyObjectBuilder_Door test = new MyObjectBuilder_Door() {BuildPercent= 100 , 
                                                                                    Orientation = d_Orient,
                                                                                    CustomName = "Portal Block", 
                                                                                    Min = new VRageMath.Vector3I(0,0,0)};
                            //Same as above but for base block
                            MyObjectBuilder_CubeBlock bat = new MyObjectBuilder_CubeBlock() { BlockOrientation = b_Orient, 
                                                                                              BuildPercent = 100, 
                                                                                              Orientation = quad, 
                                                                                              SubtypeName = "LargeBlockArmorBlock", 
                                                                                              Min = new VRageMath.Vector3I(0,-1,0) };
                            
                            //Creates a cubegrid builder(a ship) 
                            MyObjectBuilder_CubeGrid cube = new MyObjectBuilder_CubeGrid();
                            
                            //cube.CubeBlocks.Add(test);// adds the door to the cube grid
                            //cube.CubeBlocks.Add(bat);// adds the block under the door
                            
                            //cube.PersistentFlags = MyPersistentEntityFlags2.InScene;//sets the cubegrid as visisble and existant

                            */
                            
                            //test code for debuging/ future reference  DO NOT DELETE in this branch
                            //VRageMath.Quaternion e_orient;
                            //entrance_g.Orientation.GetQuaternion(out e_orient);
                           ///MyAPIGateway.Utilities.ShowMessage("Debug", "W "+e_orient.W + " X " + e_orient.X + " Y " + e_orient.Y + " Z " + e_orient.Z + " ");
                            //foreach(var cubbie in cube.CubeBlocks)
                           // {
                                //MyAPIGateway.Utilities.ShowMessage("Debug", cubbie.BlockOrientation.Forward.ToString() +"\n" + exit_g.Orientation.Forward.ToString());                         
                            
                           // }
                           

                            var prefab = MyDefinitionManager.Static.GetPrefabDefinition("Fighter");//Find fighter prefab

                            var grid = prefab.CubeGrids[0];//get the 1st cubegrid from fighter prefab


                            grid.PositionAndOrientation = new MyPositionAndOrientation(new VRageMath.Vector3(0, 0, 0), VRageMath.Vector3.Forward, VRageMath.Vector3.Up);//set its position and orientation
                            //cube.PositionAndOrientation = new MyPositionAndOrientation(new VRageMath.Vector3(0, 0, 0), VRageMath.Vector3.Forward, VRageMath.Vector3.Up);//Set its position and orentation

                            var entity = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(grid);// Add it to the scene
                            //var newship = MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(cube);// addit to the scene( it can now be reference via the "newship" variable
                            entity.DebugDraw();
                            
                        }
                        catch
                        {
                            MyAPIGateway.Utilities.ShowNotification("Well it didnt work");
                        }

                        //test code plz ignore


                        WasUsed = true;//set portal wasused to true

                        isactive = false;//turn off isactive, starts cooldown

                    }
                }
            }
        }





        //creating list of portals
        private List<Sandbox.ModAPI.IMySlimBlock> GetGateList()//list of portals, called up by GetGateList()
        {
            HashSet<IMyEntity> hash = new HashSet<IMyEntity>();//Creates new IMyEntity hash set

            List<Sandbox.ModAPI.IMySlimBlock> gateList = new List<Sandbox.ModAPI.IMySlimBlock>();//create new list of blocks

            Sandbox.ModAPI.MyAPIGateway.Entities.GetEntities(hash, (x) => x is Sandbox.ModAPI.IMyCubeGrid);//puts all cube grids in hash
            

            foreach (var entity in hash)//for each entity in hash
            {

                Sandbox.ModAPI.IMyCubeGrid grid = entity as Sandbox.ModAPI.IMyCubeGrid;//creates grid based around each entity in hash
                
                try//try this out because if wrong it breaks game
                {
                    grid.GetBlocks(gateList, (x) => x.FatBlock is IMyDoor && (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Portal") && man.isActive(x.FatBlock));//Checks if it is an active door that contains portal
                    
                }
                catch// if the try didnt work
                {
                    MyAPIGateway.Utilities.ShowNotification("Error When trying to find Portals", 250);//say there was an error
                }
                    
            }
             
            
            return gateList;
        }//end of gatelist creation
        

        //Will rewrite bellow to handle antenna connections
        private IMyDoor GetNearestGateOnDifferentGrid(IMyDoor sourceGate)//this is the check for determining nearest active gate in list
        {
            List<Sandbox.ModAPI.IMySlimBlock> gateList = GetGateList();//gateList is gateList from above

            double distance = 0.0d;//distance variable, also makes first gate checked in the for each below relative 0

            IMyDoor nearest = null;//set nearest variable of IMyDoor to null
            
            foreach (var gate in gateList)//for each gate in gateList(every active door)
            {

                if (gate.IsDestroyed || !((gate.FatBlock as IMyDoor).Enabled)/* I think this means that the door is on, not off in settings */ || !gate.FatBlock.IsFunctional || (sourceGate.GetPosition() == gate.FatBlock.GetPosition()))//Skip disabled, or activated or destroyed gates

                    continue;//skips current gate in gateList, goes onto next gate
                    if ((distance == 0.0d || (sourceGate.GetPosition() - gate.FatBlock.GetPosition()).Length() < distance) && (sourceGate.CustomName == (gate.FatBlock as IMyTerminalBlock).CustomName)) //if it is the first gate checked or it is closest gate so far and if the thing has the same name
                    {
                        nearest = gate.FatBlock as IMyDoor;//sets this gate as the closest

                        distance = (entrance_g.GetPosition() - gate.FatBlock.GetPosition()).Length();//sets distance to length of closest gate
                    }
            }

            return nearest;//returns the closest gate checked
            
        }// end of get nearest gate


        // To Do List for tomorrow:
        //door opens when link is formed, door shuts when link is closed
        // New portal block that is not shaped like a door, has a contact zone that does not count as a wall collision, just showy.
        //Give new portal block a crafting recipe, other stuff along that line. Things it needs to function in game.
        //new portal consumes power when activated, consumes more power on teleport, and on maintaining link. When block is off portal is off
        //instead of cooldown, when someone teleports they cannot go back through the portal unless they loose contact with the portal contact zone, in which case they can re-enter
        // Inheret relative motion and tilts when passing through portal
        //portal can do other small entities, bullets, rockets, items, rocks, ect
        //Instead of a distance check, relies on antenna connection to determine where it is allowed to teleport





        //useless stuff, DO NOT DELETE 


        public override void UpdateAfterSimulation()//irrelevant
        {

        }
        public override void UpdateAfterSimulation10()//irelevant
        {


        }
        public override void UpdateBeforeSimulation()//does nothing
        {

        }
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)// Does nothing
        {
            return entrance_g as MyObjectBuilder_EntityBase;
        }


    }//end of class
    


}//end of namespace