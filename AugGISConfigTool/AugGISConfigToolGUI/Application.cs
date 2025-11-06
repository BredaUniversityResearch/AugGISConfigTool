using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.OpenGL3;
using Hexa.NET.ImGui.Backends.SDL3;
using Hexa.NET.OpenGL;
using Hexa.NET.SDL3;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;
using SDLWindow = Hexa.NET.SDL3.SDLWindow;

namespace AugGISConfigToolGUI;

public class Application
{
	private GL _gl;
	public GL GL => _gl;
	
	private string _appName;
	public string AppName => _appName;
	
	private int _windowWidth;
	public int WidowWidth => _windowWidth;
	
	private int _windowHeight;
	public int WindowHeight => _windowHeight;
	
	private uint _windowId;
	private bool _shouldClose = false;
	private const bool c_useImguiViewportFeature = true;

	protected unsafe SDLWindow* sdlWindow; 
	
	public Application(string a_appName, int a_windowWidth, int a_windowHeight)
	{
		unsafe
		{
			_appName = a_appName;
			_windowWidth = a_windowWidth;
			_windowHeight = a_windowHeight;
		
			SDL.SetHint(SDL.SDL_HINT_MOUSE_FOCUS_CLICKTHROUGH, "1");
			SDL.Init(SDLInitFlags.Events | SDLInitFlags.Video);
		
			float mainScale = SDL.GetDisplayContentScale(SDL.GetPrimaryDisplay());
			sdlWindow = SDL.CreateWindow(a_appName, (int)(_windowWidth * mainScale), (int)(_windowHeight * mainScale),
				SDLWindowFlags.Resizable | SDLWindowFlags.Opengl | SDLWindowFlags.HighPixelDensity);
			_windowId = SDL.GetWindowID(sdlWindow);

			var guiContext = ImGui.CreateContext();
			ImGui.SetCurrentContext(guiContext);

			// Setup ImGui config.
			var io = ImGui.GetIO();
			io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard; // Enable Keyboard Controls
			io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad; // Enable Gamepad Controls
			io.ConfigFlags |= ImGuiConfigFlags.DockingEnable; // Enable Docking

			if (c_useImguiViewportFeature)
			{
				io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable; // Enable Multi-Viewport / Platform Windows
				io.ConfigViewportsNoAutoMerge = false;
				io.ConfigViewportsNoTaskBarIcon = false;
			}

			var style = ImGui.GetStyle();
			style.ScaleAllSizes(mainScale);
			style.FontScaleDpi = mainScale; 
			io.ConfigDpiScaleFonts = true; 
			io.ConfigDpiScaleViewports = true;

			var context = SDL.GLCreateContext(sdlWindow);

			ImGuiImplSDL3.SetCurrentContext(guiContext);
			if (!ImGuiImplSDL3.InitForOpenGL(new SDLWindowPtr((Hexa.NET.ImGui.Backends.SDL3.SDLWindow*)sdlWindow),
				    (void*)context.Handle))
			{
				Console.WriteLine("Failed to init ImGui Impl SDL3");
				SDL.Quit();
				return;
			}

			ImGuiImplOpenGL3.SetCurrentContext(guiContext);
			if (!ImGuiImplOpenGL3.Init((byte*)null))
			{
				Console.WriteLine("Failed to init ImGui Impl OpenGL3");
				SDL.Quit();
				return;
			}

			_gl = new(new BindingsContext(sdlWindow, context));
		}
	}

	protected virtual void Render()
	{
		
	}
	
	public void Run()
	{
		SDLEvent sdlEvent = default;
		
		while (!_shouldClose)
		{
			SDL.PumpEvents();
			while (SDL.PollEvent(ref sdlEvent))
			{
				unsafe
				{
					ImGuiImplSDL3.ProcessEvent((Hexa.NET.ImGui.Backends.SDL3.SDLEvent*)&sdlEvent);
				}

				CheckInput(sdlEvent);
			}

			_gl.MakeCurrent();
			_gl.Clear(GLClearBufferMask.ColorBufferBit);

			ImGuiImplOpenGL3.NewFrame();
			ImGuiImplSDL3.NewFrame();
			ImGui.NewFrame();

			Render();

			ImGui.Render();
			ImGui.EndFrame();

			_gl.MakeCurrent();
			ImGuiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

			if (c_useImguiViewportFeature)
			{
				ImGui.UpdatePlatformWindows();
				ImGui.RenderPlatformWindowsDefault();
			}

			_gl.MakeCurrent();
			_gl.SwapBuffers();
		}
	}

	protected virtual void CheckInput(SDLEvent a_sdlEvent)
	{
		switch ((SDLEventType)a_sdlEvent.Type)
		{
			case SDLEventType.Quit:
				_shouldClose = true;
				break;
			case SDLEventType.Terminating:
				_shouldClose = true;
				break;
			case SDLEventType.WindowCloseRequested:
				var windowEvent = a_sdlEvent.Window;
				if (windowEvent.WindowID == _windowId)
				{
					_shouldClose = true;
				}
				break;
		}
	}
}