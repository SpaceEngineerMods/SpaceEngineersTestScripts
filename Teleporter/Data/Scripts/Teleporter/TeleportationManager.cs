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

namespace Teleporter
{
    //Does the teleportaion Process
    //Controls the cool down of the portals
    public class TeleportationManager : MyGameLogicComponent
    {
        //String storing the EntityIds of inactive portals
        static String DisabledPortals = "";
        
        public bool Teleportplayer(IMyDoor entrance_p, IMyDoor exit_p, Sandbox.ModAPI.Interfaces.IMyControllableEntity player)
        {
            if (entrance_p == null || exit_p == null)
                return false;
            
            if (DisabledPortals.Contains(exit_p.EntityId.ToString()) || DisabledPortals.Contains(entrance_p.EntityId.ToString()))
            {
                MyAPIGateway.Utilities.ShowNotification("Portals Are Disabled");
                MyAPIGateway.Utilities.ShowNotification(DisabledPortals, 2000);
                return false;
            }
            else
            {

                MyAPIGateway.Utilities.ShowNotification("This is a portal", 1000, MyFontEnum.Red);
                VRageMath.Vector3 pos = exit_p.GetPosition();
                if (player.Entity.EntityId == MyAPIGateway.Session.Player.PlayerCharacter.Entity.EntityId)
                {
                    pos += (exit_p.WorldMatrixNormalizedInv.Forward * 2);
                    pos += (exit_p.WorldMatrixNormalizedInv.Down * 2);
                }
                else
                {
                    pos += (exit_p.WorldMatrixNormalizedInv.Forward * 10);
                }

                player.Entity.GetTopMostParent().SetPosition(pos);

                // TODO set player orientation
                // Enable gate shutdown timer
                DisabledPortals += " " + exit_p.EntityId + " " + entrance_p.EntityId;
                MyAPIGateway.Utilities.ShowNotification(DisabledPortals, 2000);
                return true;
            }  
        }
        //Removes the specified portal from the disabled list
        public void ActivatePortal(IMyDoor portal)
        {
            if(!DisabledPortals.Contains(portal.EntityId.ToString()))
                return;

            int len = portal.EntityId.ToString().Length;
            int indexofportal = DisabledPortals.IndexOf(portal.EntityId.ToString());
            if(indexofportal != -1)
                DisabledPortals = DisabledPortals.Remove(indexofportal, len);
            

        }

        public void GetInactivePortals()
        {
            MyAPIGateway.Utilities.ShowNotification(DisabledPortals, 2000);
        }

    }
}
