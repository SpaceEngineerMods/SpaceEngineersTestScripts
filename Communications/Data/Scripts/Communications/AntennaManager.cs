﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRageMath;
using IMyCubeBlock = Sandbox.ModAPI.IMyCubeBlock;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMyFunctionalBlock = Sandbox.ModAPI.Ingame.IMyFunctionalBlock;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

namespace Communications
{
    //Does the teleportaion Process
    //Controls the cool down of the portals
    public class AntennaManager //creates teleportation manager
    {
        private List<IMySlimBlock> _commMainList = new List<IMySlimBlock>(); //create new list of blocks
        private List<IMySlimBlock> _commPortList = new List<IMySlimBlock>(); //create new list of blocks
        private List<IMySlimBlock> _workingAntennas = new List<IMySlimBlock>(); //create new list of blocks
        private List<IMySlimBlock> _workingAntennaTurrets = new List<IMySlimBlock>(); //create new list of blocks

        private void BlocksUpdate()
        {
            {
                var hash = new HashSet<IMyEntity>(); //Creates new IMyEntity hash set

                MyAPIGateway.Entities.GetEntities(hash, x => x is IMyCubeGrid); //puts all cube grids in hash
                _commMainList.Clear();
                _commPortList.Clear();
                _workingAntennaTurrets.Clear();
                _workingAntennas.Clear();
                foreach (var grid in hash.Select(entity => entity as IMyCubeGrid))
                {
                    try //try this out because if wrong it breaks game
                    {
                        if (grid != null)
                            grid.GetBlocks(_commPortList,
                                x =>
                                    x.FatBlock is IMyTextPanel &&
                                    (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Comm") && (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Port") && IsActive((IMyFunctionalBlock)x.FatBlock))
                                ; //Checks if it is an active Comm that contains portal
                    }
                    catch
                    {
                        // ignored
                    }
                    try //try this out because if wrong it breaks game
                    {
                        if (grid != null)
                            grid.GetBlocks(_commMainList,
                                x =>
                                    x.FatBlock is IMyTextPanel &&
                                    (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Comm") &&
                                    (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Main") && IsActive((IMyFunctionalBlock)x.FatBlock));
                        //Checks if it is an active Comm that contains portal
                    }
                    catch
                    {
                        // ignored
                    }
                    try //try this out because if wrong it breaks game
                    {
                        if (grid != null)
                            grid.GetBlocks(_workingAntennas, x => x.FatBlock is IMyRadioAntenna && IsActive((IMyFunctionalBlock) x.FatBlock));
                        //Checks if it is an active Comm that contains portal
                    }
                    catch
                    {
                        // ignored
                    }
                    try //try this out because if wrong it breaks game
                    {
                        if (grid != null)
                            grid.GetBlocks(_workingAntennaTurrets, x => x.FatBlock is IMyLaserAntenna && IsActive((IMyFunctionalBlock) x.FatBlock));
                        //Checks if it is an active Comm that contains portal
                    }
                    catch
                    {
                        // ignored
                    }
                }
            } //end of commlist creation
        }

        public IEnumerable<HashSet<IMyEntity>> GetValidConnections()
        {
            BlocksUpdate();
            var connections = new HashSet<HashSet<IMyEntity>>(); //Creates new IMyEntity hash set
            
            foreach (var antenna in _workingAntennas)
            {
               
                var subConnections = new HashSet<IMyEntity> {antenna.FatBlock};
                var position = antenna.FatBlock.GetPosition();
                var radius = ((IMyRadioAntenna) antenna.FatBlock).Radius;

                var antenna1 = antenna;
                foreach (var subAntenna in _workingAntennas.Where(subAntenna => (position - (subAntenna.FatBlock.GetPosition())).Length() <= radius &&
                                                                                antenna1.FatBlock.GetTopMostParent().EntityId != subAntenna.FatBlock.GetTopMostParent().EntityId))
                {
                    subConnections.Add(subAntenna.FatBlock);
                }
                connections.Add(subConnections);
            }

            foreach (var turret in _workingAntennaTurrets)
            {
                var subConnections = new HashSet<IMyEntity> {turret.FatBlock};

                foreach (var subTurret in _workingAntennaTurrets)
                {
                    if (((IMyLaserAntenna) turret.FatBlock).TargetCoords == (subTurret.FatBlock.GetPosition()))
                        subConnections.Add(subTurret.FatBlock);
                }
                connections.Add(subConnections);
            }
            return connections;
        }

        public List<IMyTextPanel> GetAvailableCommPortList(IMyCubeGrid referenceGrid,String channel)
        {
            //Update the lists
            BlocksUpdate();
           
            // get the antenna connecitons
            var antennaConnections = GetValidConnections();

            //find to see what conneciton the antenna is connected to should only  return a single list
            var availableAntennasHash = antennaConnections.Where(x => x.First().GetTopMostParent().EntityId == referenceGrid.EntityId);
            var availableAntennas = new List<IMyEntity>();
           // removing rendunant top list
            var antennasfirstOrDefault = availableAntennasHash.FirstOrDefault();

            //null checking
            if (antennasfirstOrDefault != null)
            availableAntennas.AddRange(antennasfirstOrDefault);


            var commList = new List<IMyTextPanel>();
            var commSlimBlockList = new List<IMySlimBlock>();

            //adding the antennas to a list
            foreach(var antenna in availableAntennas)
               commSlimBlockList.AddRange(_commPortList.Where(comm1 => (comm1.FatBlock.GetTopMostParent().EntityId == antenna.GetTopMostParent().EntityId || comm1.FatBlock.GetTopMostParent().EntityId == referenceGrid.EntityId)  && GetChannel(comm1.FatBlock as IMyTerminalBlock) == channel));
            
            //polymorphism is great
            commSlimBlockList.ForEach(comm => commList.Add(comm.FatBlock as IMyTextPanel));


            return commList;

        }


        public String GetChannel(IMyTerminalBlock commpanel)
        {
            var name = commpanel.DisplayNameText;
            var index = 0;
            var channel = "";
           
            index = name.IndexOf("-");

            if (index <= 0)
            {
                return channel;
            }
            
            channel = name.Substring(index, (name.Length - index));
            MyAPIGateway.Utilities.ShowMessage("test", channel + " " + name);
            return channel;
        }

        private static bool IsActive(IMyFunctionalBlock comm) //checks whether a portal is active or not
        {
            return comm.IsFunctional && comm.Enabled;
        }
    }
}