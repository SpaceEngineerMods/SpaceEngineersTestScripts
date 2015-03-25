using System;
using System.Collections.Generic;

using System.Linq;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
using IMyCubeBlock = Sandbox.ModAPI.IMyCubeBlock;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

//Basic imports

namespace Communications //teleporter namespace
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel))] //Type of TextPanel, applies to TextPanels
    public class CommLink : MyGameLogicComponent
    //class CommLink, calls from game logic, further describes what a CommLink is
    {
        private List<IMySlimBlock> OreDetectors = new List<IMySlimBlock>(); //create new list of blocks
        private List<IMySlimBlock> Asteroids = new List<IMySlimBlock>(); //create new list of blocks
        private List<IMySlimBlock> OreDeposits = new List<IMySlimBlock>(); //create new list of blocks
        private bool _isComm; //Is it a communications panel?
        private int _mTimer; //timer
        private MyObjectBuilder_EntityBase _objectBuilder;
       
        private AntennaManager _antennaManager;
        private IMyTextPanel _commPanel; //for use later
        

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        //initializes object, overwrites original object code making it an communications panel
        {
            _objectBuilder = objectBuilder;
            _commPanel = Entity as IMyTextPanel; //says this entity is an IMyTextPanel Called entrance_g

            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
            //Door state updated every 10th/ 100th frame

            _antennaManager = new AntennaManager();
        }

        public override void UpdateAfterSimulation10()
        {
           
            if (!_isComm) return;
            if (_commPanel.DisplayNameText.Contains("Ship"))
            {

                var fullString = "";

                _commPanel.WritePublicText(fullString);
                _commPanel.WritePublicText(fullString);
                _commPanel.GetTopMostParent().Physics.UpdateAccelerations();

                var shipPosition = _commPanel.GetTopMostParent().GetPosition();
                shipPosition = new VRageMath.Vector3D(Math.Round(shipPosition.X, 4), Math.Round(shipPosition.Y, 4), Math.Round(shipPosition.Z, 4));

                var shipVelocity = _commPanel.GetTopMostParent().Physics.LinearVelocity;
                shipVelocity.X = (float)Math.Round(shipVelocity.X, 4);
                shipVelocity.Y = (float)Math.Round(shipVelocity.Y, 4);
                shipVelocity.Z = (float)Math.Round(shipVelocity.Z, 4);

                var shipAcceleration = _commPanel.GetTopMostParent().Physics.LinearAcceleration;
                shipAcceleration.X = (float)Math.Round(shipAcceleration.X, 4);
                shipAcceleration.Y = (float)Math.Round(shipAcceleration.Y, 4);
                shipAcceleration.Z = (float)Math.Round(shipAcceleration.Z, 4);

                var shipAngle = _commPanel.GetTopMostParent().WorldMatrix.GetOrientation();

                var shipRotation = _commPanel.GetTopMostParent().Physics.AngularVelocity;
                shipRotation.X = (float)Math.Round(shipRotation.X, 4);
                shipRotation.Y = (float)Math.Round(shipRotation.Y, 4);
                shipRotation.Z = (float)Math.Round(shipRotation.Z, 4);

                var shipRotationAcceleration = _commPanel.GetTopMostParent().Physics.AngularAcceleration;
                shipRotationAcceleration.X = (float)Math.Round(shipRotationAcceleration.X, 4);
                shipRotationAcceleration.Y = (float)Math.Round(shipRotationAcceleration.Y, 4);
                shipRotationAcceleration.Z = (float)Math.Round(shipRotationAcceleration.Z, 4);

                var radianAngle = new Vector3D(Math.Round(Math.Atan2(shipAngle.M32, shipAngle.M33), 4),
                        Math.Round(Math.Atan2(-shipAngle.M31, Math.Sqrt(Math.Pow(shipAngle.M32, 2) + Math.Pow(shipAngle.M33, 2))), 4),
                    Math.Round(Math.Atan2(shipAngle.M21, shipAngle.M11), 4));

                fullString = "Ship Pos " + shipPosition + "\n Ship Vel " + shipVelocity + "\n Ship Accel " + shipAcceleration + "\n Ship Angle "
                    + radianAngle + "\n Ship Rot " + shipRotation + "\n Ship Rot Accel " + shipRotationAcceleration;

                _commPanel.WritePublicText(fullString);
                _commPanel.ShowPublicTextOnScreen();
                _commPanel.SetValueFloat("FontSize", 1.0f);

            }
        }

        //This is the actual check for teleportation
        public override void UpdateBeforeSimulation100() //rewriting the Comm update stuff, activating every 10 frames
        {
            var myname = _commPanel.DisplayNameText; //create string myname, name of Comm
            _isComm = myname.Contains("Comm");
            if (!_isComm) return;
            
           
            if (myname.Contains("Main"))
            {
                _commPanel.WritePublicText("");
                var validConnections = _antennaManager.GetValidConnections();
                var shipName = "";
                int number = 0;
                foreach (var hash in validConnections)
                {
                    shipName += "\n Connections #" + number + "\n";

                    shipName = hash.Aggregate(shipName, (current, antenna) => current + ((antenna as Sandbox.ModAPI.IMyTerminalBlock).CustomName + "\n"));
                    number++;
                }
              
                _commPanel.WritePublicText(number +"\n" + shipName);
                _commPanel.ShowPublicTextOnScreen();
                _commPanel.SetValueFloat("FontSize", 1.0f);
                //not done
            }
           
            if (myname.Contains("Port"))
            {
                //not done
            }
            if (myname.Contains("Ore"))
            {
                GetOre();
                //not done
            }
        }

        private void GetOre()
        {
            var ship = (_commPanel.GetTopMostParent() as IMyCubeGrid);
            ship.GetBlocks(OreDetectors, x =>
            {
                var myTerminalBlock = x.FatBlock as IMyTerminalBlock;
                return myTerminalBlock != null && (x.FatBlock is IMyOreDetector && this.IsActive(x.FatBlock));
            });
            
        }

        private bool IsActive(IMyCubeBlock comm) //checks whether a portal is active or not
        {
            return comm.IsWorking || comm.IsFunctional;
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