using System.Text;
using AugGISDataParser;
using DotSpatial.Data;
using DotSpatial.Data.Properties;
using DotSpatial.Symbology;
using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using Hexa.NET.Utilities;
using ImGui = Hexa.NET.ImGui.ImGui;
using SDLEvent = Hexa.NET.SDL3.SDLEvent;

namespace AugGISConfigToolGUI;

internal class AugGISConfigToolGUIApp : Application
{
	private bool m_isOpen = false;
	private SettingsDataModel loadedSettingsDataModel = null;

	private OpenFileDialogHandle _openGisFolderHandle = new OpenFileDialogHandle();
	private OpenFileDialogHandle _openSettingsFileHandle = new OpenFileDialogHandle();
	private OpenFileDialogHandle _exportConfigFileHandle = new OpenFileDialogHandle();
	private OpenFileDialogHandle _saveSettingsFileHandle = new OpenFileDialogHandle();

	static void Main()
	{
		AugGISConfigToolGUIApp app = new AugGISConfigToolGUIApp("AugGIS Config Tool", 1200, 700);
		app.Run();
	}
	
	private AugGISConfigToolGUIApp(string a_appName, int a_windowWidth, int a_windowHeight) : base(a_appName,
		a_windowWidth, a_windowHeight)
	{
		m_isOpen = true;

		unsafe
		{
			ImGuiIOPtr io = ImGui.GetIO();
			io.Fonts.AddFontFromFileTTF("assets/fonts/Roboto-Regular.ttf", 18);
		}
	}

	protected override void Render()
	{
		base.Render();
		unsafe
		{
			GL.ClearColor(1, 0.8f, 0.75f, 1);

			ImGuiDrawToolBar();

			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			ImGui.SetNextWindowPos(viewport.WorkPos);
			ImGui.SetNextWindowSize(viewport.WorkSize);

			if (ImGui.Begin("AugGis Config Tool", ref m_isOpen,
				    ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.NoSavedSettings))
			{
				if (loadedSettingsDataModel != null)
				{
					ImGui.BeginChild("Settings");

					ImGuiAugGisDrawer.DrawSettingsDataModel(loadedSettingsDataModel);

					if (ImGui.Button("Save Settings"))
					{
						ImGuiAugGisDrawer.ShowOpenFileDialog(sdlWindow, _saveSettingsFileHandle);
					}

					if (ImGui.Button("Export to config file"))
					{
						ImGuiAugGisDrawer.ShowOpenFolderDialog(sdlWindow, _exportConfigFileHandle);
					}

					ImGui.EndChild();
				}
				ImGui.End();
			}

			CheckFileHandles();
			//ImGui.ShowDemoWindow();
		}
	}

	protected override void CheckInput(SDLEvent a_sdlEvent)
	{
		base.CheckInput(a_sdlEvent);
	}

	private unsafe void ImGuiDrawToolBar()
	{
		ImGui.BeginMainMenuBar();

		if (ImGui.BeginMenu("File"))
		{
			if (ImGui.MenuItem("Open GIS Data Folder"))
			{
				ImGuiAugGisDrawer.ShowOpenFolderDialog(sdlWindow, _openGisFolderHandle);
			}

			if (ImGui.MenuItem("Open Settings File"))
			{
				ImGuiAugGisDrawer.ShowOpenFileDialog(sdlWindow, _openSettingsFileHandle);
			}

			ImGui.EndMenu();
		}

		ImGui.EndMainMenuBar();
	}

	private void CheckFileHandles()
	{
		if (_openGisFolderHandle.hasFinished)
		{
			try
			{
				loadedSettingsDataModel =
					SettingsDataCreator.CreateSettingsDataModelFromGisData(_openGisFolderHandle.pickedPath);
				_openGisFolderHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				ImGuiAugGisDrawer.ShowErrorPopupModal("Invalid GIS Folder", "Ok",
					() => _openGisFolderHandle.hasFinished = false);
			}
		}

		if (_openSettingsFileHandle.hasFinished)
		{
			try
			{
				loadedSettingsDataModel =
					SettingsDataCreator.LoadSettingsDataModelFromFile(_openSettingsFileHandle.pickedPath);
				_openSettingsFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				ImGuiAugGisDrawer.ShowErrorPopupModal("Invalid Settings File", "Ok",
					() => _openSettingsFileHandle.hasFinished = false);
			}
		}

		if (_exportConfigFileHandle.hasFinished)
		{
			try
			{
				JsonConfigObject configObject =
					ConfigDataCreator.CreateConfigDataModelFromSettings(loadedSettingsDataModel);
				ConfigDataCreator.SaveConfigObjectToFile(configObject, _exportConfigFileHandle.pickedPath);
				_exportConfigFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				ImGuiAugGisDrawer.ShowErrorPopupModal("Invalid config path!", "Ok",
					() => { _exportConfigFileHandle.hasFinished = false; });
			}
		}

		if (_saveSettingsFileHandle.hasFinished)
		{
			try
			{
				SettingsDataCreator.SaveSettingsDataModelToFile(loadedSettingsDataModel,
					_saveSettingsFileHandle.pickedPath);
				_saveSettingsFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				ImGuiAugGisDrawer.ShowErrorPopupModal("Invalid Settings Path", "Ok",
					() => _saveSettingsFileHandle.hasFinished = false);
			}
		}
	}
}