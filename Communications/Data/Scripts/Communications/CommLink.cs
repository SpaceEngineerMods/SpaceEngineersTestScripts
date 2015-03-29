using System;
using System.Collections.Generic;

using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Screens.Helpers;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;
using IMyCubeBlock = Sandbox.ModAPI.IMyCubeBlock;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

//Basic imports

//ToDO:
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
        public List<Vector3> OrePositions;  //create new list of blocks

        private String _mostRecentText;
        private DateTime _mostRecentTime;

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
            _mostRecentTime = DateTime.MinValue;
            OrePositions = new List<Vector3>();
        }
        public override void UpdateAfterSimulation()
        {
            Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
        }

        public override void UpdateAfterSimulation10()
        {
           
            if (!_isComm) return;
            if (_commPanel.DisplayNameText.Contains("Ship"))
                ShipInfo();
        }

        //This is the actual check for teleportation
        public override void UpdateBeforeSimulation100() //rewriting the Comm update stuff, activating every 10 frames
        {
            var myname = _commPanel.DisplayNameText; //create string myname, name of Comm

            _isComm = myname.Contains("Comm");

            if (!_isComm) return;


            if (myname.Contains("Main"))
                GetAntenna();

            if (myname.Contains("Port"))
                CommPort(); //testing code


            
        

     
            if (myname.Contains("Ore"))
            {
                OreDetectors.Clear();
                ValidAsteroids.Clear();
                try
                {
                    MyAPIGateway.Entities.GetEntities(Asteroids, x => x is IMyVoxelMap);
                    var ship = (_commPanel.GetTopMostParent() as IMyCubeGrid);
                    ship.GetBlocks(OreDetectors, x =>
                    {
                        var myTerminalBlock = x.FatBlock as IMyTerminalBlock;
                        return myTerminalBlock != null && (x.FatBlock is IMyOreDetector);
                    });
                    string returnStr = "Test";
                    MyAPIGateway.Utilities.ShowMessage("Test 1", OreDetectors.Count.ToString());
                    foreach (var oreDetector in OreDetectors)
                    {
                        MyAPIGateway.Utilities.ShowMessage("Test 2", OreDetectors.Count.ToString());
                        var detector = oreDetector.FatBlock as IMyOreDetector;
                        if (detector != null)
                            returnStr += oreDetector.Position.ToString() + " TESTING" + detector.Range.ToString();
                        if (oreDetector != null)
                        {
                            float Radius = 0;
                            var myOreDetector = oreDetector.FatBlock as IMyOreDetector;
                            if (myOreDetector != null)
                            {
                                Radius = myOreDetector.Range;
                            }
                            var asteroidPosition = oreDetector.FatBlock.GetPosition();

                            foreach (
                                var asteroid1 in
                                    Asteroids.Where(
                                        asteroid1 => (asteroidPosition - asteroid1.GetPosition()).Length() <= 10000))
                            {
                                if ((asteroid1 as IMyVoxelMap).DoOverlapSphereTest(Radius, asteroidPosition))
                                {
                                    
                                    IMyStorage testStorage = (asteroid1 as IMyVoxelMap).Storage;
                                    
                                    MyAPIGateway.Utilities.ShowMessage("test", (asteroid1 as IMyVoxelMap).StorageName);
                                    
                                    MyAPIGateway.Utilities.ShowMessage("Test 3", OreDetectors.Count.ToString());
                                    ValidAsteroids.Add(asteroid1);
                                    List<IMyEntity> asteroid1ChildernList = new List<IMyEntity>();
                                    asteroid1.GetChildren(asteroid1ChildernList, x => x != null);
                                    asteroid1ChildernList.ForEach(asteroidchildren => OrePositions.Add(asteroidchildren.GetPosition()));

                                    byte[] test;
                                    testStorage.Save(out test);
                                    int i = 0;
                                    foreach (var x in test)
                                    {
                                        MyAPIGateway.Utilities.ShowMessage("test byte" + i, x.ToString());
                                        i++;
                                    }
                                  
                                    
                                    MyAPIGateway.Utilities.ShowMessage("Child Test", testStorage.Size.ToString());
                                   
                                }
                            }
                        }
                    }
                    returnStr += "\n\n";
                    returnStr = ValidAsteroids.Aggregate(returnStr,
                        (current, asteroid) => current + (asteroid.WorldAABB.Center.ToString() + "\n "));
                    returnStr += "\n\n";
                    returnStr = OrePositions.Aggregate(returnStr, (current, ore) => current + ore.ToString() + " ore");
                    _commPanel.WritePublicText(returnStr);
                    _commPanel.ShowPublicTextOnScreen();
                    _commPanel.SetValueFloat("FontSize", 1.0f);
                }
                catch
                {
                    //ignored
                }
            }
        }

           private void GetAntenna()
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

            _commPanel.WritePublicText(number + "\n" + shipName);
            _commPanel.ShowPublicTextOnScreen();
            _commPanel.SetValueFloat("FontSize", 1.0f);
            //not done
        }

        private static bool IsActive(IMyCubeBlock comm) //checks whether a portal is active or not
        {
            return comm.IsWorking || comm.IsFunctional;
        }

       
        private void CommPort()
        {
            var time = DateTime.Now;
            var commportlist = _antennaManager.GetAvailableCommPortList(_commPanel.GetTopMostParent() as IMyCubeGrid , _antennaManager.GetChannel(_commPanel as IMyTerminalBlock));
            
            
           
            foreach (var comm in commportlist)
            {
                long commUpdate;
                try
                {
                    commUpdate = Convert.ToInt64(comm.GetPublicTitle());
                }
                catch
                {
                    comm.WritePublicTitle("00000000000");
                    comm.ShowPublicTextOnScreen();
                    continue;
                }
                if (commUpdate - _mostRecentTime.Ticks < 0)
                {
                    comm.WritePublicText(_mostRecentText);
                    comm.WritePublicTitle(_mostRecentTime.Ticks.ToString());
                    comm.ShowPublicTextOnScreen();
                    continue;
                }

              
                  
            }
            if (_commPanel.GetPublicText() != _mostRecentText)
            {
                _mostRecentTime = time;
                _mostRecentText = _commPanel.GetPublicText();
                _commPanel.WritePublicTitle(time.Ticks.ToString());
                //MyAPIGateway.Utilities.ShowMessage("Console Test","Most Recent Time " + _mostRecentText);
                _commPanel.ShowPublicTextOnScreen();
            }

            

        }


        private void ShipInfo()
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
      
        
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false) // Does nothing
        {
            return _objectBuilder;
        }
    } //end of class
} //end of namespace