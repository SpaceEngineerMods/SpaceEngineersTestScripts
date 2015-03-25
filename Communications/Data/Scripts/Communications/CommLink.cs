using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection.Emit;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;

//Basic imports

namespace Communications //teleporter namespace
{
    [MyEntityComponentDescriptor(typeof (MyObjectBuilder_TextPanel))] //Type of TextPanel, applies to TextPanels
    public class CommLink : MyGameLogicComponent
        //class CommLink, calls from game logic, further describes what a CommLink is
    {
        private AntennaManager _antennaManager;
        private MyObjectBuilder_EntityBase _objectBuilder;
        //private AntennaManager AM = new AntennaManager();
        private IMyTextPanel commPanel; //for use later
        

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
            //initializes object, overwrites original object code making it an communications panel
        {
            _objectBuilder = objectBuilder;
            commPanel = Entity as IMyTextPanel; //says this entity is an IMyTextPanel Called entrance_g

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
                //Door state updated every 10th/ 100th frame

            _antennaManager = new AntennaManager();
        }

        public override void UpdateAfterSimulation10()
        {
           
        }

        //This is the actual check for teleportation
        public override void UpdateBeforeSimulation100() //rewriting the Comm update stuff, activating every 10 frames
        {
            var myname = commPanel.DisplayNameText; //create string myname, name of Comm

            if (!myname.Contains("Comm"))
            {
               return;

            }

            
            if (myname.Contains("Main"))
            {
                commPanel.WritePublicText("");
                var validConnections = _antennaManager.GetValidConnections();
                var shipName = "";
                int number = 0;
                foreach (var hash in validConnections)
                {
                    shipName += "\n Connections #" + number + "\n";

                    shipName = hash.Aggregate(shipName, (current, antenna) => current + ((antenna as Sandbox.ModAPI.IMyTerminalBlock).CustomName + "\n"));
                    number++;
                }
              
                commPanel.WritePublicText(number +"\n" + shipName);
                commPanel.ShowPublicTextOnScreen();
                commPanel.SetValueFloat("FontSize", 1.0f);
                //not done
            }
            if (myname.Contains("Ship"))
            {
                

                var fullString = "";
                
                commPanel.WritePublicText(fullString);
                

                commPanel.GetTopMostParent().Physics.UpdateAccelerations();
                var shipPosition = commPanel.GetTopMostParent().GetPosition().ToString();
                var shipVelocity = commPanel.GetTopMostParent().Physics.LinearVelocity.ToString();
                var shipAcceleration = commPanel.GetTopMostParent().Physics.LinearAcceleration.ToString();
                var shipAngle = commPanel.GetTopMostParent().WorldMatrix.GetOrientation();
                var shipRotation = commPanel.GetTopMostParent().Physics.AngularVelocity.ToString();
                var shipRotationAcceleration = commPanel.GetTopMostParent().Physics.AngularAcceleration.ToString();
                var eulerAngle = new Vector3D(Math.Atan2(shipAngle.M32, shipAngle.M33),
                        Math.Atan2(-shipAngle.M31, Math.Sqrt( Math.Pow(shipAngle.M32, 2) + Math.Pow(shipAngle.M33, 2))),
                    Math.Atan2(shipAngle.M21, shipAngle.M11));
                    fullString = "Ship Pos " + shipPosition + "\n Ship Vel " + shipVelocity + "\n Ship Accel " + shipAcceleration + "\n Ship Angle " + eulerAngle + "\n Ship Rot Vel" + shipRotation + "\n Ship Rot Accel " + shipRotationAcceleration;
                

                commPanel.WritePublicText(fullString);
                commPanel.ShowPublicTextOnScreen();
                commPanel.SetValueFloat("FontSize", 1.0f);
               
            }
            if (myname.Contains("Port"))
            {
                //not done
            }

         
        }

        public override void UpdateAfterSimulation()
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        /*if (isportal && isactive)//if isportal is true
            {
                    if (man.Teleportplayer(entrance_g, exit_g, player))//no idea what this says
                    {
                        WasUsed = true;//set portal wasused to true

                        isactive = false;//turn off isactive, starts cooldown

                    }
                }
            }*/
        //creating list of portals
        /*public List<Sandbox.ModAPI.IMySlimBlock> GetCommList()//list of Valid Communication textpanels, called up by GetcommList()
        {
            HashSet<IMyEntity> hash = new HashSet<IMyEntity>();//Creates new IMyEntity hash set

            List<Sandbox.ModAPI.IMySlimBlock> commList = new List<Sandbox.ModAPI.IMySlimBlock>();//create new list of blocks

            Sandbox.ModAPI.MyAPIGateway.Entities.GetEntities(hash, (x) => x is Sandbox.ModAPI.IMyCubeGrid);//puts all cube grids in hash

            foreach (var entity in hash)//for each entity in hash
            {

                Sandbox.ModAPI.IMyCubeGrid grid = entity as Sandbox.ModAPI.IMyCubeGrid;//creates grid based around each entity in hash

                try//try this out because if wrong it breaks game
                {
                    grid.GetBlocks(commList, (x) => x.FatBlock is IMyTextPanel && (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Comm") && man.isActive(x.FatBlock));//Checks if it is an active Comm that contains portal

                }
                catch// if the try didnt work
                {
                    MyAPIGateway.Utilities.ShowNotification("Error When trying to find Comms", 250);//say there was an error
                }

            }


            return commList;
        }//end of commlist creation

        //Will rewrite bellow to handle antenna connections
        private IMyTextPanel GetNearestcommOnDifferentGrid(IMyTextPanel sourcecomm)//this is the check for determining nearest active comm in list
        {
            List<Sandbox.ModAPI.IMySlimBlock> commList = GetCommList();//commList is commList from above

            double distance = 0.0d;//distance variable, also makes first comm checked in the for each below relative 0

            IMyTextPanel nearest = null;//set nearest variable of IMyTextPanel to null

            foreach (var comm in commList)//for each comm in commList(every active Comm)
            {

                if (comm.IsDestroyed || !((comm.FatBlock as IMyTextPanel).Enabled)/* I think this means that the Comm is on, not off in settings  || !comm.FatBlock.IsFunctional || (sourcecomm.GetPosition() == comm.FatBlock.GetPosition()))//Skip disabled, or activated or destroyed comms

                    continue;//skips current comm in commList, goes onto next comm
                if ((distance == 0.0d || (sourcecomm.GetPosition() - comm.FatBlock.GetPosition()).Length() < distance) && (sourcecomm.CustomName == (comm.FatBlock as IMyTerminalBlock).CustomName)) //if it is the first comm checked or it is closest comm so far and if the thing has the same name
                {
                    nearest = comm.FatBlock as IMyTextPanel;//sets this comm as the closest

                    distance = (comm.GetPosition() - comm.FatBlock.GetPosition()).Length();//sets distance to length of closest comm
                }
            }

            return nearest;//returns the closest comm checked

        }// end of get nearest comm
        */
        //useless stuff, DO NOT DELETE 

        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false) // Does nothing
        {
            return _objectBuilder;
        }
    } //end of class
} //end of namespace