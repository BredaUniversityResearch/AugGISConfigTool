using Hexa.NET.SDL3;

namespace AugGISConfigToolGUI;

public class Application
{
	private string _appName;
	private int _windowWidth;
	private int _windowHeight;

	public Application(string a_appName, int a_windowWidth, int a_windowHeight)
	{
		_appName = a_appName;
		_windowWidth = a_windowWidth;
		_windowHeight = a_windowHeight;
	}
}