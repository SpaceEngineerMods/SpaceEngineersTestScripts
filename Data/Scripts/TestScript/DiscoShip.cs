using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 
using Sandbox.Common;
using Sandbox.ModAPI;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
[MyEntityComponentDescriptor(typeof(MyObjectBuilder_CubeGrid))]
public class MyDiscoShip : MyGameLogicComponent
{
public override void Close() { }
public override void Init(MyObjectBuilder_EntityBase objectBuilder)
{
Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_100TH_FRAME;
}
public override void MarkForClose() { }
public override void UpdateAfterSimulation() { }
public override void UpdateAfterSimulation10() { }
public override void UpdateAfterSimulation100() { }
public override void UpdateBeforeSimulation() { }
public override void UpdateBeforeSimulation10()
{
var cubeGrid = (Sandbox.ModAPI.IMyCubeGrid)Entity;
if (cubeGrid.DisplayName.Contains("Megadisco"))
{
DateTime randomizer = DateTime.Now;
int R = (randomizer.Millisecond + randomizer.Second * randomizer.Millisecond) % 255;
int G = (randomizer.Second * randomizer.Second * randomizer.Millisecond) % 255;
int B = (randomizer.Millisecond + randomizer.Second + randomizer.Second * randomizer.Millisecond) % 255;
var c = VRageMath.ColorExtensions.ColorToHSV(VRageMath.Color.FromNonPremultiplied(R, G, B, 255));
cubeGrid.ColorBlocks(cubeGrid.Min, cubeGrid.Max, c);
}
}
public override void UpdateBeforeSimulation100()
{
var cubeGrid = (Sandbox.ModAPI.IMyCubeGrid)Entity;
if (cubeGrid.DisplayName.Contains("Disco"))
{
DateTime randomizer = DateTime.Now;
int R = (randomizer.Millisecond + randomizer.Second * randomizer.Millisecond) % 255;
int G = (randomizer.Second * randomizer.Second * randomizer.Millisecond) % 255;
int B = (randomizer.Millisecond + randomizer.Second + randomizer.Second * randomizer.Millisecond) % 255;
var c = VRageMath.ColorExtensions.ColorToHSV(VRageMath.Color.FromNonPremultiplied(R, G, B, 255));
cubeGrid.ColorBlocks(cubeGrid.Min, cubeGrid.Max, c);
}
}
public override void UpdateOnceBeforeFrame() { }
}