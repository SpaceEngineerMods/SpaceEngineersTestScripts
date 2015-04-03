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
//Fix Ore Detection
//Clean up code

namespace Communications //teleporter namespace
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TextPanel))] //Type of TextPanel, applies to TextPanels
    public class CommLink : MyGameLogicComponent
    //class CommLink, calls from game logic, further describes what a CommLink is
    {
        public List<IMySlimBlock> OreDetectors = new List<IMySlimBlock>(); //create new list of blocks
        public HashSet<IMyEntity> Asteroids = new HashSet<IMyEntity>(); //create new list of blocks
        public HashSet<IMyEntity> ValidAsteroids = new HashSet<IMyEntity>(); //create new list of blocks

        private String _mostRecentText;// Timers for the comm panel
        private DateTime _mostRecentTime;

        private bool _isComm; //Is it a communications panel?
        public int MTimer;
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
            //_mostRecentTime = DateTime.MinValue;
        }
        public override void UpdateAfterSimulation()//Says when our block should update
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation10()//Every 10 frames update our ship info
        {
           
            if (!_isComm) return;// if we have Comm in the name
            if (_commPanel.DisplayNameText.Contains("Ship"))//and Ship in the name
            {
                ShipInfo();//Run program Ship
            }
        }
        
        //This is the actual check for teleportation
        public override void UpdateBeforeSimulation100() //rewriting the Comm update stuff, activating every 100 frames
        {
            var myname = _commPanel.DisplayNameText; //create string myname, name of Comm

            _isComm = myname.Contains("Comm");//do we have ship in the name

            if (!_isComm) return;//if not do nothing


            if (myname.Contains("Main"))//If we have Main in the name
                GetAntenna();//run the antenna code

            if (myname.Contains("Port"))//do we have port in the name
                CommPort(); //rum the Port code

            if (myname.Contains("Ore"))//Do we have Ore in the name
            {
                OreDetection();//Run the ore Code
                MyAPIGateway.Utilities.ShowMessage("Ore:"," Running");//Debug stuff
                AsteroidManager.Update100();//Update the Timer on the Asteroid Manager function
            }
        }
        
        private void OreDetection()//Supposed to return a list of Asteroids, Ore positions and materials, WIP
        {
            String storageStr = null;//Storage String for ore values
                OreDetectors.Clear();//resets
                ValidAsteroids.Clear();
                Asteroids.Clear();
            var oreList = new List<AsteroidManager.OreCoord>();//Create new Ore List, to receive or list from Asteroid Manager Function
            MyAPIGateway.Entities.GetEntities(Asteroids, x => x is IMyVoxelMap); //getting asteroids

            var ship = (_commPanel.GetTopMostParent() as IMyCubeGrid); //getting our ship

            if (ship != null)
                ship.GetBlocks(OreDetectors, x => //getting ore detectors from ship
                {

                    var myTerminalBlock = x.FatBlock as IMyTerminalBlock;

                    return myTerminalBlock != null && (x.FatBlock is IMyOreDetector);

                });
            MyAPIGateway.Utilities.ShowMessage("Ore:", " Detector Count " + OreDetectors.Count);//Debug or detectors
            foreach (var asteroid1 in from oreDetector in OreDetectors//For every ore Detector and Every Asteroid
                let myOreDetector = oreDetector.FatBlock as IMyOreDetector//what Ore Detector is
                let radius = myOreDetector.Range//Get Radius of Ore Detectors
                let asteroidPosition = oreDetector.FatBlock.GetPosition()//Position of Asteroid
                from asteroid1 in Asteroids.Where(//If asteroid is less that 10000 M, do the more computationally extensive test
                    asteroid1 => (asteroidPosition - asteroid1.GetPosition()).Length() <= 10000)//of whether or not it is actually
                    .Where(asteroid1 => ((IMyVoxelMap) asteroid1).DoOverlapSphereTest(radius, asteroidPosition))// in range
                    .Cast<IMyVoxelMap>()
                select asteroid1)
            {
                ValidAsteroids.Add(asteroid1);//add the asteroid to valid asteroids if it passed the test
                oreList = AsteroidManager.GetGrid(asteroid1);//Get ore list from antenna manager, that class is new and DOES NOT WORK
                storageStr = oreList.Aggregate(storageStr,//adds next ore list to storage string
                (current, ore) => current + (ore.VMaterial + " " + ore.OrePos + "\n"));
            }
            var returnStr = "\n\n";
            MyAPIGateway.Utilities.ShowMessage("Ore:", " Valid Asteroids " + ValidAsteroids.Count);//Debug Script
            returnStr = ValidAsteroids.Aggregate(returnStr,
                (current, asteroid) => current + (asteroid.WorldAABB.Center.ToString() + "\n "));//Add asteroids to the main string
            returnStr += "\n\n";
            returnStr += storageStr;//add ores to the main string
            _commPanel.WritePublicText(returnStr);//write text on screen
            _commPanel.ShowPublicTextOnScreen();//show the screen
            _commPanel.SetValueFloat("FontSize", 1.0f);//font
        }
    
           private void GetAntenna()//asks for the list of all connections
            {
                _commPanel.WritePublicText("");
                var validConnections = _antennaManager.GetValidConnections();//calls from antenna manager
                var shipName = "";
                int number = 0;
                foreach (var hash in validConnections)//generates the text
                {
                    shipName += "\n Connections #" + number + "\n";

                    shipName = hash.Aggregate(shipName,
                        (current, antenna) => current + (((IMyTerminalBlock) antenna).CustomName + "\n"));
                    number++;
                }

                _commPanel.WritePublicText(number + "\n" + shipName);//print to screen
                _commPanel.ShowPublicTextOnScreen();//show screen
                _commPanel.SetValueFloat("FontSize", 1.0f);//font
            //not done
        }
    
        public static bool IsActive(IMyCubeBlock comm) //checks whether a something is powered and not damaged
        {
            return comm.IsWorking || comm.IsFunctional;
        }

       
        private void CommPort()//Comm port script, who is talking to who
        {
            var time = DateTime.Now;// Date Time update used for determining who right over who(last updated)
            var commportlist = _antennaManager.GetAvailableCommPortList(_commPanel.GetTopMostParent() as IMyCubeGrid , _antennaManager.GetChannel(_commPanel as IMyTerminalBlock));
            //Calls from antenna manager
            
           
            foreach (var comm in commportlist)//For each comm
            {
                long commUpdate;
                try
                {
                    commUpdate = Convert.ToInt64(comm.GetPublicTitle());
                }
                catch
                {
                    comm.WritePublicTitle("00000000000");//assign a numbered identity
                    comm.ShowPublicTextOnScreen();
                    continue;
                }
                if (commUpdate - _mostRecentTime.Ticks < 0)//if comm is not up to date
                {
                    comm.WritePublicText(_mostRecentText);
                    comm.WritePublicTitle(_mostRecentTime.Ticks.ToString());//update displayed string
                    comm.ShowPublicTextOnScreen();
                }

              
                  
            }
            if (_commPanel.GetPublicText() != _mostRecentText)//if the text is not up to date
            {
                _mostRecentTime = time;
                _mostRecentText = _commPanel.GetPublicText();//what the most recent text is
                _commPanel.WritePublicTitle(time.Ticks.ToString());
                //MyAPIGateway.Utilities.ShowMessage("Console Test","Most Recent Time " + _mostRecentText);
                _commPanel.ShowPublicTextOnScreen();
            }

            

        }

    
        private void ShipInfo()//Ship info
        {
            var fullString = "";//setting up string

            _commPanel.GetTopMostParent().Physics.UpdateAccelerations();//update the physics before we call it up

            var shipPosition = _commPanel.GetTopMostParent().GetPosition();//returns ship position
            shipPosition = new Vector3D(Math.Round(shipPosition.X, 4), Math.Round(shipPosition.Y, 4), Math.Round(shipPosition.Z, 4));
            //round it
            var shipVelocity = _commPanel.GetTopMostParent().Physics.LinearVelocity;//return and round linear velocity
            shipVelocity.X = (float)Math.Round(shipVelocity.X, 4);
            shipVelocity.Y = (float)Math.Round(shipVelocity.Y, 4);
            shipVelocity.Z = (float)Math.Round(shipVelocity.Z, 4);

            var shipAcceleration = _commPanel.GetTopMostParent().Physics.LinearAcceleration;//return and round linear acceleration
            shipAcceleration.X = (float)Math.Round(shipAcceleration.X, 4);
            shipAcceleration.Y = (float)Math.Round(shipAcceleration.Y, 4);
            shipAcceleration.Z = (float)Math.Round(shipAcceleration.Z, 4);

            var shipAngle = _commPanel.GetTopMostParent().WorldMatrix.GetOrientation();//return angles

            var shipRotation = _commPanel.GetTopMostParent().Physics.AngularVelocity;//return and round angular velocity
            shipRotation.X = (float)Math.Round(shipRotation.X, 4);
            shipRotation.Y = (float)Math.Round(shipRotation.Y, 4);
            shipRotation.Z = (float)Math.Round(shipRotation.Z, 4);

            var shipRotationAcceleration = _commPanel.GetTopMostParent().Physics.AngularAcceleration;//return and round angular acceleration
            shipRotationAcceleration.X = (float)Math.Round(shipRotationAcceleration.X, 4);
            shipRotationAcceleration.Y = (float)Math.Round(shipRotationAcceleration.Y, 4);
            shipRotationAcceleration.Z = (float)Math.Round(shipRotationAcceleration.Z, 4);

            var radianAngle = new Vector3D(Math.Round(Math.Atan2(shipAngle.M32, shipAngle.M33), 4),
                    Math.Round(Math.Atan2(-shipAngle.M31, Math.Sqrt(Math.Pow(shipAngle.M32, 2) + Math.Pow(shipAngle.M33, 2))), 4),
                Math.Round(Math.Atan2(shipAngle.M21, shipAngle.M11), 4));//calculate angles (all from pi to -pi) and round to 4

            fullString = "Ship Pos " + shipPosition + "\n Ship Vel " + shipVelocity + "\n Ship Accel " + shipAcceleration + "\n Ship Angle "
                + radianAngle + "\n Ship Rot " + shipRotation + "\n Ship Rot Accel " + shipRotationAcceleration;//convert to string

            _commPanel.WritePublicText(fullString);//write the text
            _commPanel.ShowPublicTextOnScreen();//update screen
            _commPanel.SetValueFloat("FontSize", 1.0f);//font size

        }
      
        
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false) // Does nothing, needed because we inhereted from something else
        {
            return _objectBuilder;
        }
    } //end of class
} //end of namespace