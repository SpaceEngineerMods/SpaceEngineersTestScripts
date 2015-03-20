void Main()  
{  
var ScreenUp = GridTerminalSystem.GetBlockWithName("ScreenUp") as IMyTextPanel; 
var ScreenDown = GridTerminalSystem.GetBlockWithName("ScreenDown") as IMyTextPanel; 
var Reactor = GridTerminalSystem.GetBlockWithName("Reactor") as IMyReactor;  
var Sound = GridTerminalSystem.GetBlockWithName("Sound") as IMySoundBlock;  
  
bool Objective = false;  
if(ScreenUp == null || ScreenDown== null || Reactor == null|| Sound == null)
return;
  
Objective = Reactor.IsFunctional && Reactor.IsWorking; 
  
  
if(Objective)  
{  
Sound.ApplyAction("PlaySound");  
ScreenUp.WritePublicText("Objective");  
ScreenUp.SetValue("FontSize",9.5f);  
ScreenUp.ShowPublicTextOnScreen();  
ScreenDown.WritePublicText("Complete");  
ScreenDown.SetValue("FontSize",9.5f);  
ScreenDown.ShowPublicTextOnScreen();  
}  
else  
{   

ScreenUp.WritePublicText("test");  
ScreenUp.SetValue("FontSize",9.5f);  
ScreenUp.ShowPublicTextOnScreen();  
ScreenDown.WritePublicText("test");  
ScreenDown.SetValue("FontSize",9.5f);  
ScreenDown.ShowPublicTextOnScreen();  
 
}  
  
  
}