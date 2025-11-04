using Hexa.NET.ImGui;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;

namespace AugGISConfigToolGUI;

internal class AugGISConfigToolGUIApp : Application
{
	private AugGISConfigToolGUIApp(string a_appName, int a_windowWidth, int a_windowHeight) : base(a_appName,
		a_windowWidth, a_windowHeight)
	{

	}
	
	protected override void Render()
	{
		base.Render();
		
		GL.ClearColor(1, 0.8f, 0.75f, 1);
		ImGui.ShowDemoWindow();
	}

	protected override void CheckInput(SDLEvent a_sdlEvent)
	{
		base.CheckInput(a_sdlEvent);
	}

	static void Main()
	{
		AugGISConfigToolGUIApp app = new AugGISConfigToolGUIApp("AugGIS Config Tool", 1200, 700);
		app.Run();
	}
}