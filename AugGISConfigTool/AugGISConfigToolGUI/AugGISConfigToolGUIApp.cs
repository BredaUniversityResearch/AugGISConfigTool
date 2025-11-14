using System.Text;
using AugGISDataParser;
using DotSpatial.Data;
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
			ImGui.PushID("TypeSettings");
			ImGuiDrawRasterLayerTypeSettings(a_rasterLayerSetting);
			ImGui.PopID();
			
			ImGui.PushID("MappingSettings");
			ImGuiDrawRasterLayerMappingSettings(a_rasterLayerSetting);
			ImGui.PopID();
			
			ImGui.PushID("ScaleSettings");
			ImGuiDrawRasterLayerScaleSettings(a_rasterLayerSetting);
			ImGui.PopID();
			
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

	private void ImGuiDrawRasterLayerTypeSettings(RasterLayerSetting a_rasterLayerSetting)
	{
		if (ImGui.Button("+"))
		{
			a_rasterLayerSetting.rasterLayerTypes.Add(new LayerType());
		}
		ImGui.SameLine();
		if (ImGui.TreeNode("Types"))
		{
			for (int i = a_rasterLayerSetting.rasterLayerTypes.Count - 1; i >= 0; i--)
			{
				ImGui.PushID(i);
				if (ImGui.Button("-"))
				{
					a_rasterLayerSetting.rasterLayerTypes.RemoveAt(i);
					ImGui.PopID();
					continue;
				}
				ImGui.SameLine();
				ImGui.InputText("Type", ref a_rasterLayerSetting.rasterLayerTypes[i].name, (nuint)255);
				ImGui.PopID();
			}
			ImGui.TreePop();
		}
	}

	private void ImGuiDrawRasterLayerMappingSettings(RasterLayerSetting a_rasterLayerSetting)
	{
		if (ImGui.Button("+"))
		{
			a_rasterLayerSetting.rasterMappings.Add(new RasterMapping());
		}
		ImGui.SameLine();
		if (ImGui.TreeNode("Mappings"))
		{
			for (int i = a_rasterLayerSetting.rasterMappings.Count - 1; i >= 0; i--)
			{
				ImGui.PushID(i);
				if (ImGui.Button("-"))
				{
					a_rasterLayerSetting.rasterMappings.RemoveAt(i);
					ImGui.PopID();
					continue;
				}
				ImGui.SameLine();
				if (ImGui.TreeNode("Mapping"))
				{
					RasterMapping currentMapping = a_rasterLayerSetting.rasterMappings[i];
					ImGui.InputInt("Min",  ref currentMapping.min);
					ImGui.InputInt("Max",  ref currentMapping.max);
					string previewName = a_rasterLayerSetting.rasterLayerTypes.Count == 0? "##" :  a_rasterLayerSetting.rasterLayerTypes[currentMapping.typeIndex].name;
					if(ImGui.BeginCombo("Choose Type", previewName))
					{
						foreach (LayerType type in a_rasterLayerSetting.rasterLayerTypes)
						{
							string selectableLabel = type.name == String.Empty ? "##" : type.name;
							if (ImGui.Selectable(selectableLabel))
							{
								currentMapping.typeIndex = a_rasterLayerSetting.rasterLayerTypes.IndexOf(type);
							}	
						}
						ImGui.EndCombo();
					}
					ImGui.TreePop();
				}
				ImGui.PopID();
			}
			ImGui.TreePop();
		}
	}
	
	private void ImGuiDrawRasterLayerScaleSettings(RasterLayerSetting a_rasterLayerSetting)
	{
		if (ImGui.Button("+"))
		{
			a_rasterLayerSetting.rasterScales.Add(new RasterScale());
		}
		ImGui.SameLine();
		if (ImGui.TreeNode("Scale Settings"))
		{
			for (int i = a_rasterLayerSetting.rasterScales.Count - 1; i >= 0; i--)
			{
				ImGui.PushID(i);
				if (ImGui.Button("-"))
				{
					a_rasterLayerSetting.rasterScales.RemoveAt(i);
					ImGui.PopID();
					continue;
				}
				ImGui.SameLine();
				if (ImGui.TreeNode("Scale"))
				{
					RasterScale currentScale = a_rasterLayerSetting.rasterScales[i];
					ImGui.InputInt("Min",  ref currentScale.minValue);
					ImGui.InputInt("Max",  ref currentScale.maxValue);
					if(ImGui.BeginCombo("Interpolation Type", currentScale.interpolation.ToString()))
					{
						for (int enumIndex = 0; enumIndex < (int)RasterScale.EInterpolation.Count; enumIndex++)
						{
							RasterScale.EInterpolation currentInterpolation = (RasterScale.EInterpolation)enumIndex;
							if (ImGui.Selectable(currentInterpolation.ToString()))
							{
								currentScale.interpolation = (RasterScale.EInterpolation)enumIndex;
							}	
						}
						ImGui.EndCombo();
					}
					ImGui.TreePop();
				}
				ImGui.PopID();
			}
			
			ImGui.TreePop();
		}
	}
	
	static void Main()
	{
		AugGISConfigToolGUIApp app = new AugGISConfigToolGUIApp("AugGIS Config Tool", 1200, 700);
		app.Run();
	}
}