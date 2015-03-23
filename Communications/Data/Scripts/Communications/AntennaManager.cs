using System.Collections.Generic;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRageMath;
using IMyCubeBlock = Sandbox.ModAPI.IMyCubeBlock;
using IMyCubeGrid = Sandbox.ModAPI.IMyCubeGrid;
using IMySlimBlock = Sandbox.ModAPI.IMySlimBlock;
using IMyTerminalBlock = Sandbox.ModAPI.IMyTerminalBlock;

namespace Communications
{
    //Does the teleportaion Process
    //Controls the cool down of the portals
    public class AntennaManager //creates teleportation manager
    {
        public List<IMySlimBlock> CommMainList = new List<IMySlimBlock>(); //create new list of blocks
        public List<IMySlimBlock> CommPortList = new List<IMySlimBlock>(); //create new list of blocks
        public List<IMySlimBlock> WorkingAntennas = new List<IMySlimBlock>(); //create new list of blocks
        public List<IMySlimBlock> WorkingAntennaTurrets = new List<IMySlimBlock>(); //create new list of blocks

        public void BlocksUpdate()
        {
            {
                var hash = new HashSet<IMyEntity>(); //Creates new IMyEntity hash set

                MyAPIGateway.Entities.GetEntities(hash, x => x is IMyCubeGrid); //puts all cube grids in hash

                foreach (var entity in hash) //for each entity in hash
                {
                    var grid = entity as IMyCubeGrid; //creates grid based around each entity in hash

                    try //try this out because if wrong it breaks game
                    {
                        grid.GetBlocks(CommPortList,
                            x =>
                                x.FatBlock is IMyTextPanel &&
                                (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Comm") && (x.FatBlock as IMyTerminalBlock)
                                .CustomName.Contains("Port") && this.IsActive(x.FatBlock))
                        ; //Checks if it is an active Comm that contains portal
                    }
                    catch
                    {
                       
                    }
                    try //try this out because if wrong it breaks game
                    {
                        grid.GetBlocks(CommPortList,
                            x =>
                                x.FatBlock is IMyTextPanel &&
                                (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Comm") &&
                                (x.FatBlock as IMyTerminalBlock).CustomName.Contains("Main") && this.IsActive(x.FatBlock));
                            //Checks if it is an active Comm that contains portal
                    }
                    catch
                    {
                    }
                    try //try this out because if wrong it breaks game
                    {
                        grid.GetBlocks(CommPortList, x => x.FatBlock is IMyRadioAntenna && this.IsActive(x.FatBlock));
                            //Checks if it is an active Comm that contains portal
                    }
                    catch
                    {
                    }
                    try //try this out because if wrong it breaks game
                    {
                        grid.GetBlocks(CommPortList, x => x.FatBlock is IMyLaserAntenna && this.IsActive(x.FatBlock));
                            //Checks if it is an active Comm that contains portal
                    }
                    catch
                    {
                    }
                }
            } //end of commlist creation
        }

        public HashSet<HashSet<IMyEntity>> GetValidConnections()
        {
            BlocksUpdate();
            var connections = new HashSet<HashSet<IMyEntity>>(); //Creates new IMyEntity hash set
            foreach (var antenna in WorkingAntennas)
            {
                var subConnections = new HashSet<IMyEntity> {antenna.FatBlock};
                var position = antenna.FatBlock.WorldAABB.Center;
                var radius = ((IMyRadioAntenna) antenna.FatBlock).Radius;
                
                foreach (var subAntenna in WorkingAntennas)
                {
                    if ((position - (subAntenna.FatBlock.WorldAABB.Center)).Length() <= radius &&
                        antenna.FatBlock.GetTopMostParent().EntityId != subAntenna.FatBlock.GetTopMostParent().EntityId)
                    {
                        subConnections.Add(subAntenna.FatBlock);
                    }
                }
                connections.Add(subConnections);
            }
            foreach (var turret in WorkingAntennaTurrets)
            {
                var subConnections = new HashSet<IMyEntity> {turret.FatBlock};
                foreach (var subTurret in WorkingAntennaTurrets)
                {
                    if (((IMyLaserAntenna) turret.FatBlock).TargetCoords == (subTurret.FatBlock.WorldAABB.Center))
                        subConnections.Add(subTurret.FatBlock);
                }
                connections.Add(subConnections);
            }
            return connections;
        }

        public bool IsActive(IMyCubeBlock comm) //checks whether a portal is active or not
        {
            return comm.IsWorking || comm.IsFunctional;
        }
    }
}