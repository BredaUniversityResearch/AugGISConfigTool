using Hexa.NET.SDL3;

namespace AugGISConfigToolGUI;

internal unsafe class BindingsContext : HexaGen.Runtime.IGLContext
{
	private readonly SDLWindow* _window;
	private readonly SDLGLContext _context;

	public BindingsContext(SDLWindow* a_window, SDLGLContext a_context)
	{
		this._window = a_window;
		this._context = a_context;
	}

	public nint Handle => (nint)_window;

	public bool IsCurrent => SDL.GLGetCurrentContext() == _context;

	public void Dispose()
	{
	}

	public nint GetProcAddress(string a_procName)
	{
		return (nint)SDL.GLGetProcAddress(a_procName);
	}

	public bool IsExtensionSupported(string a_extensionName)
	{
		return SDL.GLExtensionSupported(a_extensionName);
	}

	public void MakeCurrent()
	{
		SDL.GLMakeCurrent(_window, _context);
	}

	public void SwapBuffers()
	{
		SDL.GLSwapWindow(_window);
	}

	public void SwapInterval(int a_interval)
	{
		SDL.GLSetSwapInterval(a_interval);
	}

	public bool TryGetProcAddress(string a_procName, out nint a_procAddress)
	{
		a_procAddress = (nint)SDL.GLGetProcAddress(a_procName);
		return a_procAddress != 0;
	}
}