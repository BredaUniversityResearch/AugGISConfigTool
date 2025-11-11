using System.Text;
using AugGISDataParser;
using DotSpatial.Data.Properties;
using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using Hexa.NET.Utilities;
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
	
	private AugGISConfigToolGUIApp(string a_appName, int a_windowWidth, int a_windowHeight) : base(a_appName,
		a_windowWidth, a_windowHeight)
	{
		m_isOpen = true;
	}
	
	protected override void Render()
	{
		base.Render();
		unsafe
		{
			GL.ClearColor(1, 0.8f, 0.75f, 1);
			ImGui.ShowDemoWindow();
			
			ImGuiDrawToolBar();
			
			ImGui.Begin("Settings", ref m_isOpen);

			if (loadedSettingsDataModel != null)
			{
				ImGuiDrawSettings(loadedSettingsDataModel);
			}

			CheckFileHandles();
			
			ImGui.End();
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
				Util.ShowOpenFolderDialog(sdlWindow, _openGisFolderHandle);
			}
			
			if (ImGui.MenuItem("Open Settings File"))
			{
				Util.ShowOpenFileDialog(sdlWindow, _openSettingsFileHandle);
			}
			
			ImGui.EndMenu();
		}

		ImGui.EndMainMenuBar();
	}

	private unsafe void ImGuiDrawSettings(SettingsDataModel a_settingsDataModel)
	{
		ImGui.InputText("Region", ref a_settingsDataModel.region, 100);
				
		ImGui.PushItemWidth(100);
		ImGui.InputDouble("Min X", ref a_settingsDataModel.coordinate0.x);
		ImGui.SameLine();
		ImGui.InputDouble("Min Y", ref a_settingsDataModel.coordinate0.y);
		ImGui.PopItemWidth();

		if (ImGui.TreeNode("Vector Layer Settings"))
		{
			for (int i = 0; i < a_settingsDataModel.vectorLayerSettings.Count; i++)
			{
				VectorLayerSetting vectorLayerSetting = a_settingsDataModel.vectorLayerSettings[i];
				ImGuiDrawVectorLayerSettings(vectorLayerSetting, i);
			}
			ImGui.TreePop();
		}
		
		if (ImGui.TreeNode("Raster Layer Settings"))
		{
			for (int i = 0; i < a_settingsDataModel.rasterLayerSettings.Count; i++)
			{
				RasterLayerSetting rasterLayerSetting = a_settingsDataModel.rasterLayerSettings[i];
				ImGuiDrawRasterLayerSettings(rasterLayerSetting, i);
			}
			ImGui.TreePop();
		}

		if (ImGui.Button("Save Settings"))
		{
			Util.ShowOpenFileDialog(sdlWindow, _saveSettingsFileHandle);
		}
		
		if (ImGui.Button("Export to config file"))
		{
			Util.ShowOpenFileDialog(sdlWindow, _exportConfigFileHandle);
		}
	}
	
	private void ImGuiDrawVectorLayerSettings(VectorLayerSetting a_vectorLayerSetting, int a_id)
	{
		ImGui.PushID(a_id);
		if (ImGui.TreeNode(a_id.ToString(),a_vectorLayerSetting.name))
		{
			ImGuiDrawShapeTypeSelection(a_vectorLayerSetting);
			ImGui.TreePop();
		}
		ImGui.PopID();
	}

	private void ImGuiDrawShapeTypeSelection(VectorLayerSetting a_vectorLayerSetting)
	{
		if(ImGui.BeginCombo("Choose Type", a_vectorLayerSetting.type))
		{
			foreach (string key in a_vectorLayerSetting.attributeKeyToValues.Keys)
			{
				if (ImGui.Selectable(key))
				{
					a_vectorLayerSetting.type = key;
				}	
			}
			ImGui.EndCombo();
		}
	}
	
	private void ImGuiDrawRasterLayerSettings(RasterLayerSetting a_rasterLayerSetting, int a_id)
	{
		ImGui.PushID(a_id);
		if (ImGui.TreeNode(a_id.ToString(),a_rasterLayerSetting.name))
		{
			ImGui.TreePop();
		}
		ImGui.PopID();
	}
	
	private void CheckFileHandles()
	{
		if (_openGisFolderHandle.hasFinished)
		{
			try
			{
				loadedSettingsDataModel =
					SettingsDataCreator.CreateSettingsDataModelFromGisData(_openGisFolderHandle.pickedPath);
				loadedSettingsDataModel.OnAfterLoad();
				_openGisFolderHandle.hasFinished = false;
			}
			catch(Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				Util.ImGuiShowErrorPopupModal("Invalid GIS Folder", "Ok", () => _openGisFolderHandle.hasFinished = false);
			}
		}

		if (_openSettingsFileHandle.hasFinished)
		{
			try
			{
				loadedSettingsDataModel = SettingsDataCreator.LoadSettingsDataModelFromFile(_openSettingsFileHandle.pickedPath);
				loadedSettingsDataModel.OnAfterLoad();
				_openSettingsFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				Util.ImGuiShowErrorPopupModal("Invalid Settings File", "Ok", () => _openSettingsFileHandle.hasFinished = false);
			}
		}

		if (_exportConfigFileHandle.hasFinished)
		{
			try
			{
				JsonConfigObject configObject = ConfigDataCreator.CreateConfigDataModelFromSettings(loadedSettingsDataModel);
				ConfigDataCreator.SaveConfigObjectToFile(configObject, _exportConfigFileHandle.pickedPath);
				_exportConfigFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				Util.ImGuiShowErrorPopupModal("Invalid config path!", "Ok", () => { _exportConfigFileHandle.hasFinished = false;});
			}
		}
		
		if (_saveSettingsFileHandle.hasFinished)
		{
			try
			{
				SettingsDataCreator.SaveSettingsDataModelToFile(loadedSettingsDataModel, _saveSettingsFileHandle.pickedPath);
				_saveSettingsFileHandle.hasFinished = false;
			}
			catch (Exception e)
			{
				Console.Write("Error: {0} ", e.ToString());
				Util.ImGuiShowErrorPopupModal("Invalid Settings Path", "Ok", () => _saveSettingsFileHandle.hasFinished = false);
			}
		}
	}
	
	static void Main()
	{
		AugGISConfigToolGUIApp app = new AugGISConfigToolGUIApp("AugGIS Config Tool", 1200, 700);
		app.Run();
	}
}