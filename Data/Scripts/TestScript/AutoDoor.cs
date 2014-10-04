using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Common;
using Sandbox.Common.Components;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using VRageMath;

namespace AutoDoor
{
	//[MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
	//public class MyAutoDoorGlobal : MySessionComponentBase
	//{
	//	public override void BeforeStart() {
	//		List<int> adminIds = new List<int>();

	//		MyAPIGateway.
				
	//		if( 
	//	}
	//}

	/// <summary>
	/// 
	/// </summary>
	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Door))]
    public class MyAutoDoor : MyGameLogicComponent
	{
		#region Static Fields
		static System.IO.TextWriter logWriter = null;
		static int logRefCount = 0;
		Dictionary<string, DateTime> logHistory = new Dictionary<string, DateTime>();
		#endregion
		#region Constants
		const string _DoorToggleActionId = "Open";
		const bool _DoorOpen = true;
		const bool _DoorClosed = false;
		const float _DefaultDistValue = 4.5f;
		#endregion

		#region Fields
		short frameMod = 0;

		IMyDoor myDoor = null;
		Sandbox.ModAPI.IMyCubeBlock myDoorBlock = null;
		ITerminalAction toggleAction = null;

		bool _DoorState = false;
		string _LastCustomName = null;
		bool _IsActive = false;
		float distValue = _DefaultDistValue;
		#endregion

		public override void Init(Sandbox.Common.ObjectBuilders.MyObjectBuilder_EntityBase objectBuilder) {
			// Type Sandbox.Common.Components.MyEntityComponentBase used in Init not allowed in script
			logRefCount++;

			myDoor = this.Entity as IMyDoor;
			myDoorBlock = this.Entity as Sandbox.ModAPI.IMyCubeBlock;

			// hook up events
			myDoor.DoorStateChanged += MyAutoDoor_DoorStateChanged;
           
			myDoor.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;

			SetupToggleAction(); // try to setup the action

			//LogMessage("Started Logging", false);
		}
		
        
        
        public override void Close() {
			myDoor.DoorStateChanged -= MyAutoDoor_DoorStateChanged;
			BreakDownLog();
		}

		void MyAutoDoor_DoorStateChanged(bool obj) {
			if( !Entity.NeedsUpdate.HasFlag(MyEntityUpdateEnum.EACH_10TH_FRAME) ) {
				Entity.NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME | MyEntityUpdateEnum.EACH_100TH_FRAME;
			}
			_DoorState = obj;

			//LogMessage(myDoor.LocalAABB.ToString(), false);
			//LogMessage(myDoor.LocalMatrix.ToString(), false);
		}


		/// <summary>
		/// Used for open close logic
		/// </summary>
		public override void UpdateAfterSimulation10() {
			frameMod = (short)((frameMod+1) % 3);
			if( frameMod > 0 )
				return;

			if( myDoor == null )
				return;

			if( !myDoor.IsFunctional || !myDoor.IsWorking )
				return;

			//LogMessage("Looking for CustomName", false);

			string myName = myDoor.CustomName;
			if( _LastCustomName != myName ) {
				_LastCustomName = myName;

				_IsActive = myName != null && myName.Contains("AutoDoor");
				if( _IsActive ) {
					int idx = myName.LastIndexOf('-');
					if( idx >= 0 && idx + 1 < myName.Length ) {
						string partialName = myName.Substring(idx + 1);
						float newDistValue = _DefaultDistValue;
						// try to parse it
						if( float.TryParse(partialName, out newDistValue) )
							distValue = newDistValue;
						else
							distValue = _DefaultDistValue;
					}
					else
						distValue = _DefaultDistValue;
				}
			}

			if( !_IsActive )
				return;

			// cache the block position
			var myWM = myDoor.WorldMatrix;

			var myBB = myDoor.LocalAABB;
			var minVec = myBB.Min;
			var maxVec = myBB.Max;

			minVec.Z -= distValue;
			maxVec.Z += distValue;

			//minVec.X = 0;
			//maxVec.X = 0;

			minVec.Y -= 0.05f;
			maxVec.Y = 0;

			myBB.Min = minVec;
			myBB.Max = maxVec;
			var myBackBB = myBB;
			myBB = myBB.Transform(ref myWM);

			// result if any player is within range
			bool playerInRange = false;

			//LogMessage("Got to player list lookup!", false);

			List<IMyPlayer> players = new List<IMyPlayer>();

			// check to make sure players exist and then find all the players
			if( MyAPIGateway.Multiplayer != null && MyAPIGateway.Multiplayer.Players != null)
				MyAPIGateway.Multiplayer.Players.GetPlayers(players);


			//Base6Directions.Direction pForward = Base6Directions.Direction.Up;

			// look through for players in that are in range
			foreach( var player in players ) {
				// check that there is a valid character
				if( player.PlayerCharacter == null )
					continue;

				// get there position
				var charPosition = player.PlayerCharacter.Entity.GetPosition();

				//if( (myDoor.GetPosition() - charPosition).Length() < 10 ) {
				//	LogMessage(myBackBB.ToString(), true);
				//	LogMessage(myBB.ToString(), true);
				//}
	
				var result = myBB.Contains(charPosition);


				bool inRange = result == ContainmentType.Contains;//distVec.Length() <= distValue;

				//LogMessage(string.Format("inRange was {0}", inRange), true);


				if( !inRange )
					continue;

				//LogMessage(string.Format("Cen {0}", myBB.Center), true);
				//LogMessage(string.Format("Min {0}", myBB.Min), true);
				//LogMessage(string.Format("Max {0}", myBB.Max), true);

				// check for acces, we only open for friendlys
				if( !myDoor.HasPlayerAccess(player.PlayerId) )
					continue;

				//var charWM = player.PlayerCharacter.Entity.WorldMatrix;
				//pForward = Base6Directions.GetForward(ref charWM);



				//var dir = myWM.GetClosestDirection(distVec);

				
				// result
				//bool inOri = (pForward == myForward || pForward == myBackward);//&&(dir != Base6Directions.Direction.Up && dir != Base6Directions.Direction.Down);

				playerInRange = inRange; //&& inOri;

				if( playerInRange )
					break;
			}

			//LogMessage(string.Format("playerInRange was {0} and DoorState was {1}", playerInRange,_DoorState), false);

			if( toggleAction == null && !SetupToggleAction() )
				return;


			if( !playerInRange && _DoorState == _DoorOpen ) {
                MyAPIGateway.Utilities.ShowNotification(string.Format("Closing {0}", (Entity as Sandbox.ModAPI.Ingame.IMyTerminalBlock).DisplayNameText), 1000, MyFontEnum.Red);
				toggleAction.Apply(myDoorBlock);
			}

			if( playerInRange && _DoorState == _DoorClosed ) {
                MyAPIGateway.Utilities.ShowNotification(string.Format("Opening {0}", (Entity as Sandbox.ModAPI.Ingame.IMyTerminalBlock).DisplayNameText), 1000, MyFontEnum.Red);
				toggleAction.Apply(myDoorBlock);
			}
		}

		public override void UpdateAfterSimulation100() {
			var clampTimer = DateTime.Now.AddMinutes(-1);
			List<string> removeMe = new List<string>(logHistory.Count);
			foreach( var thing in logHistory ) {
				if( thing.Value < clampTimer )
					removeMe.Add(thing.Key); // we do this cause foreach doesn't like us fucking with the container
			}
			foreach( var key in removeMe )
				logHistory.Remove(key); // remove old history to keep from a memory leak
		}

		#region Logging
		void EnsureLogFile() {
			if( MyAPIGateway.Utilities == null )
				return;
			if( logWriter == null ) {
				logWriter = MyAPIGateway.Utilities.WriteFileInGlobalStorage("AutoDoor.log");
				logWriter.WriteLine("Date/Time	EntityId	Message");
			}
		}
		void BreakDownLog() {
			logRefCount--;
			if( logRefCount == 0 && logWriter != null ) {
				logWriter.Flush();
				logWriter.Dispose();
				logWriter = null;
				logHistory.Clear();
			}
		}
		public void LogMessage(string msg, bool showNotification) {
			EnsureLogFile();

			if( logWriter == null )
				showNotification = true;

			msg = string.Format("{0}	{1}", ((IMyTerminalBlock)Entity).CustomName, msg);

			if( logHistory.ContainsKey(msg) ) {
				if( logHistory[msg] > DateTime.Now.AddSeconds(-10) )
					return;

				logHistory[msg] = DateTime.Now;
			}
			else
				logHistory.Add(msg, DateTime.Now);

			if( logWriter != null ) {
				logWriter.WriteLine(string.Format("{0}	{1}", DateTime.Now, msg));
				logWriter.Flush();
			}


			if( showNotification && MyAPIGateway.Utilities != null )
				MyAPIGateway.Utilities.ShowNotification("AutoDoor: " + msg, 10000, MyFontEnum.Red);

		}
		#endregion
		bool SetupToggleAction() {
			List<ITerminalAction> actions = new List<ITerminalAction>();

			if( MyAPIGateway.TerminalActionsHelper == null )
				return false;
			MyAPIGateway.TerminalActionsHelper.GetActions(myDoor.GetType(), actions, null);

			string actionName = null;
			foreach( ITerminalAction item in actions ) {
				actionName = item.Id;
				if( _DoorToggleActionId.Equals(actionName, StringComparison.InvariantCultureIgnoreCase) ) {
					toggleAction = item;
					break;
				}
			}

			if( toggleAction == null )
				LogMessage("Couldn't find toggle action", true);

			return toggleAction != null;
		}
	}
}
