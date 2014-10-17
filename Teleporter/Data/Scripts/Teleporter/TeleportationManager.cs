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

//basic imports

namespace Teleporter
{

    //Does the teleportaion Process
    //Controls the cool down of the portals
    public class TeleportationManager: MyGameLogicComponent//creates teleportation manager
    {
        
        static String DisabledPortals = "";//stores enitity id's of the disabled portals
        
        public bool Teleportplayer(IMyDoor entrance_p, IMyDoor exit_p, Sandbox.ModAPI.Interfaces.IMyControllableEntity player)//public method that teleports a player given entrance, exit and player
        {
            //MyAPIGateway.Utilities.ShowNotification("Called Teleporter");
            if (entrance_p == null || exit_p == null)//if entrance or exit is null
                return false;//teleportation didnt happen
            //MyAPIGateway.Utilities.ShowNotification("Portal Valid");
            if (DisabledPortals.Contains(exit_p.EntityId.ToString()) || DisabledPortals.Contains(entrance_p.EntityId.ToString()))//if the entrance or exit matches with a disabled portal
                return false;//ditto

            else

            {
                //MyAPIGateway.Utilities.ShowNotification("Portals Not inactive");
                // Actual Teleportaion Code

                VRageMath.Vector3 pos = exit_p.GetPosition();//creates a 3D vector of the exit
                
                if (player.Entity.EntityId == MyAPIGateway.Session.Player.PlayerID)// checks if entity is the player going through the portal
                {

                    pos += (exit_p.WorldMatrixNormalizedInv.Forward * 2);//grabs the coordinate of exit +2

                    pos += (exit_p.WorldMatrixNormalizedInv.Down * 2);//ditto
                  
                }

                player.Entity.GetTopMostParent().SetPosition(pos);//teleports player to coordinates of exit

                // TODO set player orientation
                // Enable gate shutdown timer

                DisabledPortals += " " + exit_p.EntityId + " " + entrance_p.EntityId;// adds strings of exit and entrance to the disabled list
                MyAPIGateway.Utilities.ShowNotification("Teleporting Player");
                return true;// return true, teleportation actually happened
            }  
        }


        //Removes the specified portal from the disabled list
        public void ActivatePortal(IMyDoor portal = null)//removes portals from disabled list when 
        {
            
            if(portal == null || !DisabledPortals.Contains(portal.EntityId.ToString()) )//if portal Id isnt in disabled list
                return;//return blank

            //int len = portal.EntityId.ToString().Length;//length of the portal string

            //int indexofportal = DisabledPortals.IndexOf(portal.EntityId.ToString());//finds position of first character in a portal id

            //if(indexofportal != -1)//check if the above actually works
                //DisabledPortals = DisabledPortals.Remove(indexofportal, len);//deletes portal id from string
            

        }
        public bool isActive(Sandbox.ModAPI.IMyCubeBlock gate)//checks whether a portal is active or not
        {
            if (DisabledPortals.Contains(gate.EntityId.ToString()))//if disabled portals string contains a specific portal id
                return false;

            else
                return true;
        }
        // Does Nothing
        public override MyObjectBuilder_EntityBase GetObjectBuilder(bool copy = false)
        {
            return null;
        }
    }
}
