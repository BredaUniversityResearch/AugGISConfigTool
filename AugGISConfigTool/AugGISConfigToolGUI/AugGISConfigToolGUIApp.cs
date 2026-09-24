using System.Numerics;
using System.Text;
using AugGISDataParser;
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

	// ---- master/detail selection state ----
	private enum SelectionKind { None, Vector, Raster }
	private SelectionKind m_selectionKind = SelectionKind.None;
	private int m_selectionIndex = -1;
    private float m_sidebarWidth = 250f;
    private float m_topBarHeight = 100f;

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
			io.Fonts.AddFontFromFileTTF(
				System.IO.Path.Combine(AppContext.BaseDirectory, "assets/fonts/Roboto-Regular.ttf"), 18);
		}

		ApplyDarkTheme();
	}

	protected override void Render()
	{
		base.Render();
		unsafe
		{
			GL.ClearColor(0.06f, 0.065f, 0.07f, 1f);

			ImGuiDrawToolBar();

			ImGuiViewportPtr viewport = ImGui.GetMainViewport();
			ImGui.SetNextWindowPos(viewport.WorkPos);
			ImGui.SetNextWindowSize(viewport.WorkSize);

			bool windowOpen = ImGui.Begin("AugGis Config Tool", ref m_isOpen,
				ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoDecoration |
				ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoBringToFrontOnFocus);

			if (windowOpen)
			{
				if (loadedSettingsDataModel != null)
				{
                    ImGui.BeginChild("##topbar", new System.Numerics.Vector2(0, m_topBarHeight), ImGuiChildFlags.Borders, ImGuiWindowFlags.None);
                    DrawTopBar(loadedSettingsDataModel);
                    ImGui.EndChild();

                    // thin draggable splitter to resize the topbar height
                    ImGui.Button("##topsplitter", new System.Numerics.Vector2(ImGui.GetContentRegionAvail().X, 6f));
                    if (ImGui.IsItemActive())
                        m_topBarHeight += ImGui.GetIO().MouseDelta.Y;
                    if (ImGui.IsItemHovered() || ImGui.IsItemActive())
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);
                    m_topBarHeight = Math.Clamp(m_topBarHeight, 56f, 240f);

                    ImGui.BeginChild("##sidebar", new System.Numerics.Vector2(m_sidebarWidth, 0), ImGuiChildFlags.Borders, ImGuiWindowFlags.None);
                    DrawLayerSidebar(loadedSettingsDataModel);
                    ImGui.EndChild();

                    ImGui.SameLine();

                    // thin draggable splitter
                    ImGui.Button("##splitter_sidebar", new System.Numerics.Vector2(6f, ImGui.GetContentRegionAvail().Y));
                    if (ImGui.IsItemActive())
                        m_sidebarWidth += ImGui.GetIO().MouseDelta.X;
                    if (ImGui.IsItemHovered() || ImGui.IsItemActive())
                        ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEw);
                    m_sidebarWidth = Math.Clamp(m_sidebarWidth, 150f, 600f);

                    ImGui.SameLine();

                    ImGui.BeginChild("##content", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Borders, ImGuiWindowFlags.None);
                    DrawSelectedContent(loadedSettingsDataModel);
                    ImGui.EndChild();
                }
				else
				{
					ImGui.Spacing();
					ImGui.TextDisabled("Open a GIS data folder or a settings file from the File menu to begin.");
				}
			}

			ImGui.End();

			CheckFileHandles();
		}
	}

	protected override void CheckInput(SDLEvent a_sdlEvent)
	{
		base.CheckInput(a_sdlEvent);
	}

	// ---------------------------------------------------------------------
	// Top bar: document-level settings + global actions
	// ---------------------------------------------------------------------
	private unsafe void DrawTopBar(SettingsDataModel model)
	{
		ImGui.PushItemWidth(240);
		ImGui.InputText("Region", ref model.region, 100);
		ImGui.PopItemWidth();

		ImGui.SameLine(0, 24);
		ImGui.Checkbox("Generate Basemap", ref model.generateBasemap);

		ImGui.SameLine(0, 24);
		if (ImGui.Button("Save Settings"))
			ImGuiAugGisDrawer.ShowSaveFileDialog(sdlWindow, saveSettingsFileHandle, ImGuiAugGisDrawer.JsonFilter, 1);
		ImGui.SameLine();
		if (ImGui.Button("Export to Config"))
			ImGuiAugGisDrawer.ShowOpenFolderDialog(sdlWindow, exportConfigFileHandle);

        // four extent fields spread evenly across the available width
        float spacing = ImGui.GetStyle().ItemSpacing.X;
        float fieldW = (ImGui.GetContentRegionAvail().X - spacing * 3f) / 4f;
        fieldW = Math.Max(fieldW, 90f); // don't let them collapse to nothing

        ImGui.PushItemWidth(fieldW * 0.62f); // leave room for the label beside each box
        ImGui.InputDouble("Min X", ref model.coordinate0.x); ImGui.SameLine();
        ImGui.InputDouble("Min Y", ref model.coordinate0.y); ImGui.SameLine();
        ImGui.InputDouble("Max X", ref model.coordinate1.x); ImGui.SameLine();
        ImGui.InputDouble("Max Y", ref model.coordinate1.y);
        ImGui.PopItemWidth();
	}

	// ---------------------------------------------------------------------
	// Left sidebar: the layer list (with a colour swatch per layer)
	// ---------------------------------------------------------------------
	private void DrawLayerSidebar(SettingsDataModel model)
	{
		ImGui.SeparatorText("Vector Layers");
		for (int i = 0; i < model.vectorLayerSettings.Count; i++)
		{
			VectorLayerSetting v = model.vectorLayerSettings[i];
			ImGui.PushID(i);
            DrawLayerSwatch(VectorLayerColors(v));
			ImGui.SameLine();
			bool selected = m_selectionKind == SelectionKind.Vector && m_selectionIndex == i;
			if (ImGui.Selectable(string.IsNullOrEmpty(v.name) ? "(unnamed)" : v.name, selected))
			{
				m_selectionKind = SelectionKind.Vector;
				m_selectionIndex = i;
			}
			ImGui.PopID();
		}

		ImGui.SeparatorText("Raster Layers");
		for (int i = 0; i < model.rasterLayerSettings.Count; i++)
		{
			RasterLayerSetting r = model.rasterLayerSettings[i];
			ImGui.PushID(1000 + i);
            DrawLayerSwatch(RasterLayerColors(r));
			ImGui.SameLine();
			bool selected = m_selectionKind == SelectionKind.Raster && m_selectionIndex == i;
			if (ImGui.Selectable(string.IsNullOrEmpty(r.name) ? "(unnamed)" : r.name, selected))
			{
				m_selectionKind = SelectionKind.Raster;
				m_selectionIndex = i;
			}
			ImGui.PopID();
		}
	}

	// ---------------------------------------------------------------------
	// Content pane: editor for the selected layer (reuses existing draws)
	// ---------------------------------------------------------------------
	private void DrawSelectedContent(SettingsDataModel model)
	{
		if (m_selectionKind == SelectionKind.Vector &&
		    m_selectionIndex >= 0 && m_selectionIndex < model.vectorLayerSettings.Count)
		{
			VectorLayerSetting v = model.vectorLayerSettings[m_selectionIndex];
			ImGui.PushID("vec");
			ImGui.PushID(m_selectionIndex);
			ImGui.SeparatorText(string.IsNullOrEmpty(v.name) ? "(unnamed)" : v.name);
			ImGuiAugGisDrawer.DrawVectorLayerTypeSelection(v);
			ImGuiAugGisDrawer.DrawDynamicStringList("Tags", v.tags);
			ImGuiAugGisDrawer.DrawLayerTypesForVectorLayerSettings(v);
			ImGui.PopID();
			ImGui.PopID();
		}
		else if (m_selectionKind == SelectionKind.Raster &&
		         m_selectionIndex >= 0 && m_selectionIndex < model.rasterLayerSettings.Count)
		{
			RasterLayerSetting r = model.rasterLayerSettings[m_selectionIndex];
			ImGui.PushID("ras");
			ImGui.PushID(m_selectionIndex);
			ImGui.SeparatorText(string.IsNullOrEmpty(r.name) ? "(unnamed)" : r.name);

			ImGui.PushID("TypeSettings");
			ImGuiAugGisDrawer.DrawRasterLayerTypeSettings(r);
			ImGui.PopID();

			ImGui.PushID("TagsSettings");
			ImGuiAugGisDrawer.DrawRasterTags(r);
			ImGui.PopID();

			ImGui.PushID("MappingSettings");
			ImGuiAugGisDrawer.DrawRasterLayerMappingSettings(r);
			ImGui.PopID();

			ImGui.PushID("ScaleSettings");
			ImGuiAugGisDrawer.DrawRasterLayerScaleSettings(r);
			ImGui.PopID();

			ImGui.PopID();
			ImGui.PopID();
		}
		else
		{
			ImGui.Spacing();
			ImGui.TextDisabled("Select a layer from the left to edit it.");
		}
	}

    private static void DrawLayerSwatch(List<Vector3> a_colors)
    {
        const int maxSegments = 6;
        int n = Math.Min(a_colors.Count, maxSegments);

        float h = ImGui.GetTextLineHeight();
        System.Numerics.Vector2 size = new System.Numerics.Vector2(22f, h);
        System.Numerics.Vector2 p = ImGui.GetCursorScreenPos();
        ImDrawListPtr dl = ImGui.GetWindowDrawList();

        if (n == 0)
        {
            dl.AddRectFilled(p, new System.Numerics.	Vector2(p.X + size.X, p.Y + size.Y),
                ImGui.GetColorU32(new Vector4(0.40f, 0.42f, 0.45f, 1f))); // neutral when no types
        }
        else
        {
            float segW = size.X / n;
            for (int i = 0; i < n; i++)
            {
                System.Numerics.Vector2 a = new System.Numerics.Vector2(p.X + i * segW, p.Y);
                System.Numerics.Vector2 b = new System.Numerics.Vector2(p.X + (i + 1) * segW, p.Y + size.Y);
                dl.AddRectFilled(a, b, ImGui.GetColorU32(new Vector4(a_colors[i], 1f)));
            }
        }

        dl.AddRect(p, new System.Numerics.Vector2(p.X + size.X, p.Y + size.Y),
            ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.45f))); // subtle border

        ImGui.Dummy(size); // reserve layout space for the hand-drawn swatch
    }

    private static List<Vector3> VectorLayerColors(VectorLayerSetting v)
        => LayerColors(v.layerTypeData, GeometryOf(v.tags));

    private static List<Vector3> RasterLayerColors(RasterLayerSetting r)
        => LayerColors(r.rasterLayerTypes, "Polygon");

    // Distinct geometry-appropriate colours across a layer's types, in order of first appearance.
    private static List<Vector3> LayerColors(IEnumerable<LayerTypeData> a_types, string a_geometry)
    {
        HashSet<uint> seen = new HashSet<uint>();
        List<Vector3> colors = new List<Vector3>();
        foreach (LayerTypeData t in a_types)
        {
            AugGisConfigUtils.TryParseHexColor(GeometryColor(t, a_geometry), out uint value);
            if (seen.Add(value))
                colors.Add(AugGisConfigUtils.HexToVec3(value));
        }
        return colors;
    }

    private static string GeometryColor(LayerTypeData a_type, string a_geometry) => a_geometry switch
    {
        "Line" => a_type.lineColor,
        "Point" => a_type.pointColor,
        _ => a_type.polygonColor,
    };

    private static string GeometryOf(List<string> a_tags)
    {
        foreach (string tag in a_tags)
        {
            string t = tag.ToLowerInvariant();
            if (t.Contains("polygon")) return "Polygon";
            if (t.Contains("line")) return "Line";
            if (t.Contains("point")) return "Point";
        }
        return "Polygon";
    }

    private static Vector3 HexToColor(string a_hex)
	{
		AugGisConfigUtils.TryParseHexColor(a_hex, out uint value);
		return AugGisConfigUtils.HexToVec3(value);
	}

	private void ResetSelection(SettingsDataModel model)
	{
		if (model.vectorLayerSettings.Count > 0) { m_selectionKind = SelectionKind.Vector; m_selectionIndex = 0; }
		else if (model.rasterLayerSettings.Count > 0) { m_selectionKind = SelectionKind.Raster; m_selectionIndex = 0; }
		else { m_selectionKind = SelectionKind.None; m_selectionIndex = -1; }
	}

	// ---------------------------------------------------------------------
	// Menu bar (unchanged behaviour)
	// ---------------------------------------------------------------------
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
				ImGuiAugGisDrawer.ShowOpenFileDialog(sdlWindow, openSettingsFileHandle, ImGuiAugGisDrawer.JsonFilter, 1);
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

		bool unused_open = true;
		if (ImGui.BeginPopupModal(launchServerPopupKey, ref unused_open, ImGuiWindowFlags.AlwaysAutoResize))
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
				ResetSelection(loadedSettingsDataModel);
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
				ResetSelection(loadedSettingsDataModel);
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
				string savePath = saveSettingsFileHandle.pickedPath ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(savePath) &&
				    !savePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
				{
					savePath += ".json";
				}

				SettingsDataCreator.SaveSettingsDataModelToFile(loadedSettingsDataModel, savePath);
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

	// ---------------------------------------------------------------------
	// Dark theme
	// ---------------------------------------------------------------------
	private static unsafe void ApplyDarkTheme()
	{
		ImGui.StyleColorsDark();
		ImGuiStylePtr style = ImGui.GetStyle();

		style.WindowRounding = 0f;
		style.ChildRounding = 6f;
		style.FrameRounding = 5f;
		style.PopupRounding = 6f;
		style.GrabRounding = 4f;
		style.ScrollbarRounding = 8f;
		style.WindowPadding = new System.Numerics.Vector2(12, 12);
		style.FramePadding = new  System.Numerics.Vector2(9, 5);
		style.ItemSpacing = new  System.Numerics.Vector2(8, 7);
		style.ItemInnerSpacing = new System.Numerics.Vector2(6, 5);
		style.WindowBorderSize = 1f;
		style.ChildBorderSize = 1f;
		style.FrameBorderSize = 0f;
		style.ScrollbarSize = 12f;
		style.IndentSpacing = 18f;

		Vector4 accent   = new Vector4(0.851f, 0.376f, 0.118f, 1.00f); // burnt orange
		Vector4 accentHv = new Vector4(0.93f,  0.46f,  0.20f,  1.00f);
		Vector4 accentDim= new Vector4(0.851f, 0.376f, 0.118f, 0.45f);
		Vector4 border   = new Vector4(0.22f,  0.23f,  0.25f,  1.00f);

		style.Colors[(int)ImGuiCol.Text]                 = new Vector4(0.90f, 0.91f, 0.92f, 1.00f);
		style.Colors[(int)ImGuiCol.TextDisabled]         = new Vector4(0.52f, 0.54f, 0.57f, 1.00f);
		style.Colors[(int)ImGuiCol.WindowBg]             = new Vector4(0.100f,0.105f,0.115f,1.00f);
		style.Colors[(int)ImGuiCol.ChildBg]              = new Vector4(0.125f,0.130f,0.142f,1.00f);
		style.Colors[(int)ImGuiCol.PopupBg]              = new Vector4(0.130f,0.135f,0.150f,1.00f);
		style.Colors[(int)ImGuiCol.Border]               = border;
		style.Colors[(int)ImGuiCol.FrameBg]              = new Vector4(0.160f,0.170f,0.185f,1.00f);
		style.Colors[(int)ImGuiCol.FrameBgHovered]       = new Vector4(0.205f,0.215f,0.232f,1.00f);
		style.Colors[(int)ImGuiCol.FrameBgActive]        = new Vector4(0.240f,0.255f,0.275f,1.00f);
		style.Colors[(int)ImGuiCol.TitleBg]              = new Vector4(0.080f,0.085f,0.095f,1.00f);
		style.Colors[(int)ImGuiCol.TitleBgActive]        = new Vector4(0.080f,0.085f,0.095f,1.00f);
		style.Colors[(int)ImGuiCol.MenuBarBg]            = new Vector4(0.080f,0.085f,0.095f,1.00f);
		style.Colors[(int)ImGuiCol.Header]               = accentDim;
		style.Colors[(int)ImGuiCol.HeaderHovered]        = new Vector4(0.30f, 0.32f, 0.35f, 1.00f);
		style.Colors[(int)ImGuiCol.HeaderActive]         = accent;
		style.Colors[(int)ImGuiCol.Button]               = new Vector4(0.200f,0.215f,0.235f,1.00f);
		style.Colors[(int)ImGuiCol.ButtonHovered]        = accentHv;
		style.Colors[(int)ImGuiCol.ButtonActive]         = accent;
		style.Colors[(int)ImGuiCol.CheckMark]            = accent;
		style.Colors[(int)ImGuiCol.SliderGrab]           = accent;
		style.Colors[(int)ImGuiCol.SliderGrabActive]     = accentHv;
		style.Colors[(int)ImGuiCol.Separator]            = border;
		style.Colors[(int)ImGuiCol.SeparatorHovered]     = accentHv;
		style.Colors[(int)ImGuiCol.ScrollbarBg]          = new Vector4(0.080f,0.085f,0.095f,1.00f);
		style.Colors[(int)ImGuiCol.ScrollbarGrab]        = new Vector4(0.260f,0.270f,0.290f,1.00f);
		style.Colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.330f,0.345f,0.370f,1.00f);
	}
}
