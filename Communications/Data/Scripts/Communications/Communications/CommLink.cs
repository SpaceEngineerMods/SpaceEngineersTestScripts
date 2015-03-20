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
using VRageMath;

//Basic imports



namespace Communications//teleporter namespace
{

    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel))]//Type of TextPanel, applies to TextPanels

    public class CommLink : MyGameLogicComponent//class CommLink, calls from game logic, further describes what a CommLink is
    {
        AntennaManager AM = new AntennaManager();

        IMyTextPanel commPanel = null;//for use later

        bool isComm = false;//Is it a communications panel?

        bool isactive = true;//bool determining whether a CommLink works or not

        private int m_timer = 0;//timer

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)//initializes object, overwrites original object code making it an communications panel
        {
            commPanel = this.Entity as IMyTextPanel;//says this entity is an IMyTextPanel Called entrance_g

            commPanel.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;//Door state updated every 10th/ 100th frame

        }

        void CommSateChanged(bool obj)//sets up when and how to detect changes
        {
            if (!Entity.NeedsUpdate.HasFlag(MyEntityUpdateEnum.EACH_10TH_FRAME))//if it does not have a flag every ten frames
            {
                Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;//sets needs update to 10 or 100 frames
            }
        }

        //This is the actual check for teleportation
        public override void UpdateBeforeSimulation10() //rewriting the Comm update stuff, activating every 10 frames
        {

            string myname = commPanel.CustomName; //create string myname, name of Comm

            if (myname.Contains("Comm"))
            {
                if (myname.Contains("Main"))
                {
                    //not done
                }
                if (myname.Contains("Ship"))
                {
                    var shipPosition = commPanel.GetTopMostParent().GetPosition().ToString();
                    var shipVelocity = commPanel.GetTopMostParent().Physics.LinearVelocity.ToString();
                    var shipAcceleration = commPanel.GetTopMostParent().Physics.LinearAcceleration.ToString();
                    var shipAngle = commPanel.GetTopMostParent().WorldMatrix.GetOrientation();
                    var shipRotation = commPanel.GetTopMostParent().Physics.AngularVelocity.ToString();
                    var shipRotationAcceleration =
                        commPanel.GetTopMostParent().Physics.AngularAcceleration.ToString();
                    string FullString = shipPosition + "\n" + shipVelocity + "\n" + shipAcceleration + "\n" + shipAngle.Forward + " " + shipAngle.Up + "\n" +
                                        shipRotation + "\n" + shipRotationAcceleration;
                    (commPanel as IMyTextPanel).WritePublicText(FullString, false);
                }
                if (myname.Contains("Port"))
                {
                    //not done
                }
            }
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
            return commPanel as MyObjectBuilder_EntityBase;
        }


    }//end of class



}//end of namespace