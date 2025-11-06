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
			
			ShowToolBar();
			
			ImGui.Begin("Settings", ref m_isOpen);

			if (loadedSettingsDataModel != null)
			{
				ImGui.InputText("Region", ref loadedSettingsDataModel.region, 100);
				
				ImGui.PushItemWidth(100);
				ImGui.InputDouble("Min X", ref loadedSettingsDataModel.coordinate0.x);
				ImGui.SameLine();
				ImGui.InputDouble("Min Y", ref loadedSettingsDataModel.coordinate0.y);
				ImGui.PopItemWidth();

				if (ImGui.TreeNode("Shape Features"))
				{
					for (int i = 0; i < loadedSettingsDataModel.shapeFeatures.Count; i++)
					{
						SettingsShapeFeature shapeFeature = loadedSettingsDataModel.shapeFeatures[i];
						ImGuiDrawShapeFeature(shapeFeature, i);
					}
					ImGui.TreePop();
				}
			}
			
			ImGui.End();
		}
	}

	protected override void CheckInput(SDLEvent a_sdlEvent)
	{
		base.CheckInput(a_sdlEvent);
	}

	private unsafe void ShowToolBar()
	{
		ImGui.BeginMainMenuBar();

		if (ImGui.BeginMenu("File"))
		{
			if (ImGui.MenuItem("Open GIS Data Folder"))
			{
				SDL.ShowOpenFolderDialog(((a_userdata, a_fileList, a_filter) =>
				{
					string? selectedFolderDirectory = Utils.ToStringFromUTF8(a_fileList[0]);
					Console.WriteLine(selectedFolderDirectory);
				}), null, sdlWindow, "", false);
			}
			
			if (ImGui.MenuItem("Open Settings File"))
			{
				
				SDL.ShowOpenFileDialog((a_userdata, a_fileList, a_filter) =>
				{
					string? selectedSettingsPath = Utils.ToStringFromUTF8(a_fileList[0]);
					Console.WriteLine(selectedSettingsPath);
					
					loadedSettingsDataModel = SettingsDataCreator.LoadSettingsDataModelFromFile(selectedSettingsPath);
				},null, sdlWindow, null, 0,"", false );
			}
			
			ImGui.EndMenu();
		}

		ImGui.EndMainMenuBar();
	}
	
	public void ImGuiDrawShapeFeature(SettingsShapeFeature a_settingsShapeFeature, int a_id)
	{
		ImGui.PushID(a_id);
		if (ImGui.TreeNode(a_id.ToString(),a_settingsShapeFeature.name))
		{
			if(ImGui.BeginCombo("Choose Type", a_settingsShapeFeature.type))
			{
				foreach (string key in a_settingsShapeFeature.attributeKeyToValues.Keys)
				{
					if (ImGui.Selectable(key))
					{
						a_settingsShapeFeature.type = key;
					}	
				}
				ImGui.EndCombo();
			}
			ImGui.TreePop();
		}
		ImGui.PopID();
	}
	
	static void Main()
	{
		AugGISConfigToolGUIApp app = new AugGISConfigToolGUIApp("AugGIS Config Tool", 1200, 700);
		app.Run();
	}
}