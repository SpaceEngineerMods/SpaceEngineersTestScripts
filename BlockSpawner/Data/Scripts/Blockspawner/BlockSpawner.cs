using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Engine;
using Sandbox.Game;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using IMySensorBlock = Sandbox.ModAPI.IMySensorBlock;
namespace TestScript
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_SensorBlock))]
    class BlockSpawner : MyGameLogicComponent
    {
        

        IMySensorBlock Sensor;

        public override void Close()
        {
            Sensor.StateChanged -= sensor_StateChanged;
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            
            Sensor = Entity as IMySensorBlock;
            Sensor.StateChanged += sensor_StateChanged;
        }

        void sensor_StateChanged(bool obj)
        {
            if(!obj) return;
            MyAPIGateway.Utilities.ShowNotification("Changed States", 1000); 
            
            

            
            
           
            
            
            

            // We want to spawn ore and throw it at entity which entered sensor
            MyObjectBuilder_CubeGrid floatingBuilder = new MyObjectBuilder_CubeGrid();
            Sandbox.ModAPI.IMyCubeBlock test = new Sandbox.ModAPI.IMyCubeBlock();
            
            
            floatingBuilder.PersistentFlags = MyPersistentEntityFlags2.InScene; // Very important
            VRageMath.Vector3 pos = new VRageMath.Vector3(Sensor.Position.X * 1.5f, Sensor.Position.Y *2,Sensor.Position.Z);
                                                           
              
            test.SetPosition( pos);

            var floatingObject = Sandbox.ModAPI.MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(floatingBuilder);

            
        }

        public override void MarkForClose(){}

        public override void UpdateAfterSimulation(){}

        public override void UpdateAfterSimulation10() { }

        public override void UpdateAfterSimulation100(){}

        public override void UpdateBeforeSimulation(){ }

        public override void UpdateBeforeSimulation10(){}

        public override void UpdateBeforeSimulation100(){}
        public override void UpdateOnceBeforeFrame(){}
    
    }
}
