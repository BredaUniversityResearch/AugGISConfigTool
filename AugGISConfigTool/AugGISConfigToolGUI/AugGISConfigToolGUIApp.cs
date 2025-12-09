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

	private OpenFileDialogHandle openGisFolderHandle = new OpenFileDialogHandle();
	private OpenFileDialogHandle openSettingsFileHandle = new OpenFileDialogHandle();
	private OpenFileDialogHandle exportConfigFileHandle = new OpenFileDialogHandle();
	private OpenFileDialogHandle saveSettingsFileHandle = new OpenFileDialogHandle();

	private OpenFileDialogHandle serverBuildOpenFileDialogHandle = new OpenFileDialogHandle();
	private OpenFileDialogHandle configZipOpenFileDialogHandle = new OpenFileDialogHandle();
	const string launchServerPopupKey = "Launch Server";
	
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
						ImGuiAugGisDrawer.ShowOpenFileDialog(sdlWindow, saveSettingsFileHandle);
					}

					if (ImGui.Button("Export to config file"))
					{
						ImGuiAugGisDrawer.ShowOpenFolderDialog(sdlWindow, exportConfigFileHandle);
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
				ImGuiAugGisDrawer.ShowOpenFolderDialog(sdlWindow, openGisFolderHandle);
			}

			if (ImGui.MenuItem("Open Settings File"))
			{
				ImGuiAugGisDrawer.ShowOpenFileDialog(sdlWindow, openSettingsFileHandle);
			}

			ImGui.EndMenu();
		}
		
		if (ImGui.MenuItem("Launch Server"))
		{
			if (!ImGui.IsPopupOpen(launchServerPopupKey))
			{
				ImGui.OpenPopup(launchServerPopupKey);
			}
		}

		//passing the boolean ref here makes the popup have the close button on top right. Imgui will automatically close the popup if clicked
		bool unused_open = true;
		if (ImGui.BeginPopupModal(launchServerPopupKey, ref unused_open,ImGuiWindowFlags.AlwaysAutoResize))
		{
			ImGui.PushID(0);
			ImGuiAugGisDrawer.DrawPathBrowser(sdlWindow, serverBuildOpenFileDialogHandle, "Server Build");
			ImGui.PopID();
			ImGui.PushID(1);
			ImGuiAugGisDrawer.DrawPathBrowser(sdlWindow, configZipOpenFileDialogHandle, "Config Zip");
			ImGui.PopID();
			bool enableButton = File.Exists(serverBuildOpenFileDialogHandle.pickedPath) &&
			                      File.Exists(configZipOpenFileDialogHandle.pickedPath);
						
			ImGui.BeginDisabled(!enableButton);
			if (ImGui.Button("Launch"))
			{
				AugGISServerLauncher.ServerLauncher.LaunchServer(serverBuildOpenFileDialogHandle.pickedPath, configZipOpenFileDialogHandle.pickedPath);
				ImGui.CloseCurrentPopup();
			}
			
			ImGui.EndDisabled();
			ImGui.EndPopup();
		}
		
		ImGui.EndMainMenuBar();
	}

	private void CheckFileHandles()
	{
		if (openGisFolderHandle.hasFinished)
		{
			try
			{
				loadedSettingsDataModel =
					SettingsDataCreator.CreateSettingsDataModelFromGisData(openGisFolderHandle.pickedPath);
				openGisFolderHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				ImGuiAugGisDrawer.ShowErrorPopupModal("Invalid GIS Folder", "Ok",
					() => openGisFolderHandle.hasFinished = false);
			}
		}

		if (openSettingsFileHandle.hasFinished)
		{
			try
			{
				loadedSettingsDataModel =
					SettingsDataCreator.LoadSettingsDataModelFromFile(openSettingsFileHandle.pickedPath);
				openSettingsFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				ImGuiAugGisDrawer.ShowErrorPopupModal("Invalid Settings File", "Ok",
					() => openSettingsFileHandle.hasFinished = false);
			}
		}

		if (exportConfigFileHandle.hasFinished)
		{
			try
			{
				JsonConfigObject configObject =
					ConfigDataCreator.CreateConfigDataModelFromSettings(loadedSettingsDataModel);
				ConfigDataCreator.SaveConfigObjectToFile(configObject, exportConfigFileHandle.pickedPath);
				exportConfigFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				ImGuiAugGisDrawer.ShowErrorPopupModal("Invalid config path!", "Ok",
					() => { exportConfigFileHandle.hasFinished = false; });
			}
		}

		if (saveSettingsFileHandle.hasFinished)
		{
			try
			{
				SettingsDataCreator.SaveSettingsDataModelToFile(loadedSettingsDataModel,
					saveSettingsFileHandle.pickedPath);
				saveSettingsFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				ImGuiAugGisDrawer.ShowErrorPopupModal("Invalid Settings Path", "Ok",
					() => saveSettingsFileHandle.hasFinished = false);
			}
		}
	}
}