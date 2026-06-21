using System.Numerics;
using System.Runtime.InteropServices;
using AugGISDataParser;
using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using Hexa.NET.Utilities;

namespace AugGISConfigToolGUI;

public class OpenFileDialogHandle
{
    public bool hasFinished = false;
    public string? pickedPath = string.Empty;
    public SDLDialogFileCallback? callback; // keeps the native callback rooted so the GC can't collect it
}

public static class ImGuiAugGisDrawer
{
    // Type rows start expanded when a layer has at most this many types; above it they
    // start collapsed (the list stays a tidy overview). Expand all / Collapse all overrides either way.
    private const int TypeAutoExpandThreshold = 6;

    // --- persistent JSON filter for the file dialogs (allocated once, lives for the app's lifetime) ---
    private static readonly unsafe SDLDialogFileFilter* s_jsonFilter = CreateJsonFilter();
    public static unsafe SDLDialogFileFilter* JsonFilter => s_jsonFilter;

    private static unsafe SDLDialogFileFilter* CreateJsonFilter()
    {
        SDLDialogFileFilter* f = (SDLDialogFileFilter*)NativeMemory.Alloc((nuint)sizeof(SDLDialogFileFilter));
        f->Name = (byte*)Marshal.StringToCoTaskMemUTF8("JSON files (*.json)");
        f->Pattern = (byte*)Marshal.StringToCoTaskMemUTF8("json");
        return f;
    }

    // ---------------------------------------------------------------------
    // Dialogs
    // ---------------------------------------------------------------------
    public static void ShowErrorPopupModal(string a_message, string a_option, Action a_onClose)
    {
        string error = "Error";
        if (!ImGui.IsPopupOpen(error))
        {
            ImGui.OpenPopup(error);
        }

        if (ImGui.BeginPopupModal(error, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(a_message);
            if (ImGui.Button(a_option))
            {
                ImGui.CloseCurrentPopup();
                a_onClose?.Invoke();
            }

            ImGui.EndPopup();
        }
    }

    public static unsafe void ShowOpenFolderDialog(SDLWindow* a_window, OpenFileDialogHandle a_handle)
    {
        a_handle.callback = (a_userdata, a_fileList, a_filter) =>
        {
            if (a_fileList == null || a_fileList[0] == null) return; // cancelled or error
            a_handle.pickedPath = Utils.ToStringFromUTF8(a_fileList[0]);
            a_handle.hasFinished = true;
        };
        SDL.ShowOpenFolderDialog(a_handle.callback, null, a_window, "", false);
    }

    public static unsafe void ShowOpenFileDialog(SDLWindow* a_window, OpenFileDialogHandle a_handle)
        => ShowOpenFileDialog(a_window, a_handle, null, 0);

    public static unsafe void ShowOpenFileDialog(SDLWindow* a_window, OpenFileDialogHandle a_handle,
        SDLDialogFileFilter* a_filters, int a_filterCount)
    {
        a_handle.callback = (a_userdata, a_fileList, a_filter) =>
        {
            if (a_fileList == null || a_fileList[0] == null) return;
            a_handle.pickedPath = Utils.ToStringFromUTF8(a_fileList[0]);
            a_handle.hasFinished = true;
        };
        SDL.ShowOpenFileDialog(a_handle.callback, null, a_window, a_filters, a_filterCount, "", false);
    }

    public static unsafe void ShowSaveFileDialog(SDLWindow* a_window, OpenFileDialogHandle a_handle,
        SDLDialogFileFilter* a_filters, int a_filterCount)
    {
        a_handle.callback = (a_userdata, a_fileList, a_filter) =>
        {
            if (a_fileList == null || a_fileList[0] == null) return;
            a_handle.pickedPath = Utils.ToStringFromUTF8(a_fileList[0]);
            a_handle.hasFinished = true;
        };
        SDL.ShowSaveFileDialog(a_handle.callback, null, a_window, a_filters, a_filterCount, "");
    }

    // ---------------------------------------------------------------------
    // Vector layer editor pieces (called from the content pane)
    // ---------------------------------------------------------------------
    public static void DrawVectorLayerTypeSelection(VectorLayerSetting a_vectorLayerSetting)
    {
        if (ImGui.BeginCombo("Choose Type", a_vectorLayerSetting.selectedTypeKey))
        {
            foreach (string key in a_vectorLayerSetting.attributeKeyToValues.Keys)
            {
                if (ImGui.Selectable(key))
                {
                    if (a_vectorLayerSetting.selectedTypeKey != key)
                    {
                        a_vectorLayerSetting.layerTypeData.Clear();
                        foreach (var attribValue in a_vectorLayerSetting.attributeKeyToValues[key])
                        {
                            a_vectorLayerSetting.layerTypeData.Add(
                                new LayerTypeData() { name = attribValue?.ToString() ?? "" });
                        }
                    }

                    a_vectorLayerSetting.selectedTypeKey = key;
                }
            }

            ImGui.EndCombo();
        }
    }

    public static void DrawDynamicStringList(string a_label, List<string> a_stringList)
    {
        if (!ImGui.CollapsingHeader($"{a_label}  ({a_stringList.Count})", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.PushID(a_label);
        if (ImGui.SmallButton("+ Add"))
        {
            a_stringList.Add(string.Empty);
        }

        ImGui.Indent();
        for (int i = a_stringList.Count - 1; i >= 0; i--)
        {
            ImGui.PushID(i);
            if (ImGui.SmallButton("-"))
            {
                a_stringList.RemoveAt(i);
                ImGui.PopID();
                continue;
            }

            ImGui.SameLine();
            string currentString = a_stringList[i];
            ImGui.InputText("##entry", ref currentString, (nuint)255);
            a_stringList[i] = currentString;
            ImGui.PopID();
        }
        ImGui.Unindent();

        ImGui.PopID();
    }

    public static void DrawLayerTypesForVectorLayerSettings(VectorLayerSetting a_vectorLayerSetting)
    {
        List<LayerTypeData> types = a_vectorLayerSetting.layerTypeData;

        if (!ImGui.CollapsingHeader($"Type Data  ({types.Count})", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.PushID("Type Data Settings");

        bool? forceOpen = null;
        if (ImGui.SmallButton("Expand all")) forceOpen = true;
        ImGui.SameLine();
        if (ImGui.SmallButton("Collapse all")) forceOpen = false;

        bool autoOpen = types.Count <= TypeAutoExpandThreshold;

        for (int i = 0; i < types.Count; i++)
        {
            LayerTypeData layerTypeData = types[i];
            ImGui.PushID(i);

            DrawColorSwatch(layerTypeData.polygonColor); // swatch + SameLine

            // SetNextItemOpen must be the call right before the header (no item in between).
            if (forceOpen.HasValue)
                ImGui.SetNextItemOpen(forceOpen.Value, ImGuiCond.Always);
            else
                ImGui.SetNextItemOpen(autoOpen, ImGuiCond.FirstUseEver);

            if (ImGui.CollapsingHeader(string.IsNullOrEmpty(layerTypeData.name) ? "(unnamed)" : layerTypeData.name))
            {
                ImGui.Indent();
                DrawLayerTypeData(layerTypeData);
                ImGui.Unindent();
            }

            ImGui.PopID();
        }

        ImGui.PopID();
    }

    public static void DrawLayerTypeData(LayerTypeData a_layerTypeData)
    {
        ImGui.InputInt("Value", ref a_layerTypeData.value);
        ImGui.InputText("Description", ref a_layerTypeData.description, (nuint)255);

        ImGui.SeparatorText("Polygon");
        DrawColorInputFromHexString("Polygon Color", ref a_layerTypeData.polygonColor);
        ImGui.InputText("Polygon Pattern Name", ref a_layerTypeData.polygonPatternName, (nuint)255);

        ImGui.SeparatorText("Line");
        DrawColorInputFromHexString("Line Color", ref a_layerTypeData.lineColor);
        ImGui.InputFloat("Line Width", ref a_layerTypeData.lineWidth);
        ImGui.InputText("Line Icon", ref a_layerTypeData.lineIcon, (nuint)255);
        ImGui.InputText("Line Pattern Type", ref a_layerTypeData.linePatternType, (nuint)255);

        ImGui.SeparatorText("Point");
        DrawColorInputFromHexString("Point Color", ref a_layerTypeData.pointColor);
        ImGui.InputFloat("Point Size", ref a_layerTypeData.pointSize);
        ImGui.InputText("Point Sprite Name", ref a_layerTypeData.pointSpriteName, (nuint)255);

        ImGui.Spacing();
    }

    // ---------------------------------------------------------------------
    // Raster layer editor pieces (called from the content pane)
    // ---------------------------------------------------------------------
    public static void DrawRasterLayerTypeSettings(RasterLayerSetting a_rasterLayerSetting)
    {
        if (!ImGui.CollapsingHeader($"Types  ({a_rasterLayerSetting.rasterLayerTypes.Count})", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.PushID("RasterTypes");
        if (ImGui.SmallButton("+ Add"))
        {
            a_rasterLayerSetting.rasterLayerTypes.Add(new LayerTypeData());
        }

        ImGui.Indent();
        for (int i = a_rasterLayerSetting.rasterLayerTypes.Count - 1; i >= 0; i--)
        {
            ImGui.PushID(i);
            if (ImGui.SmallButton("-"))
            {
                a_rasterLayerSetting.rasterLayerTypes.RemoveAt(i);
                ImGui.PopID();
                continue;
            }

            ImGui.SameLine();
            ImGui.InputText("##type", ref a_rasterLayerSetting.rasterLayerTypes[i].name, (nuint)255);
            ImGui.PopID();
        }
        ImGui.Unindent();

        ImGui.PopID();
    }

    public static void DrawRasterLayerMappingSettings(RasterLayerSetting a_rasterLayerSetting)
    {
        if (!ImGui.CollapsingHeader($"Mappings  ({a_rasterLayerSetting.rasterMappings.Count})", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.PushID("RasterMappings");
        if (ImGui.SmallButton("+ Add"))
        {
            a_rasterLayerSetting.rasterMappings.Add(new RasterMapping());
        }

        for (int i = a_rasterLayerSetting.rasterMappings.Count - 1; i >= 0; i--)
        {
            ImGui.PushID(i);
            RasterMapping currentMapping = a_rasterLayerSetting.rasterMappings[i];

            ImGui.SeparatorText($"Mapping {i}");
            if (ImGui.SmallButton("- Remove"))
            {
                a_rasterLayerSetting.rasterMappings.RemoveAt(i);
                ImGui.PopID();
                continue;
            }

            ImGui.InputInt("Min", ref currentMapping.min);
            ImGui.InputInt("Max", ref currentMapping.max);

            bool validIndex = currentMapping.typeIndex >= 0 &&
                              currentMapping.typeIndex < a_rasterLayerSetting.rasterLayerTypes.Count;
            string previewName = validIndex
                ? a_rasterLayerSetting.rasterLayerTypes[currentMapping.typeIndex].name
                : "##";
            if (ImGui.BeginCombo("Type", previewName))
            {
                foreach (LayerTypeData type in a_rasterLayerSetting.rasterLayerTypes)
                {
                    string selectableLabel = type.name == string.Empty ? "##" : type.name;
                    if (ImGui.Selectable(selectableLabel))
                    {
                        currentMapping.typeIndex = a_rasterLayerSetting.rasterLayerTypes.IndexOf(type);
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.PopID();
        }

        ImGui.PopID();
    }

    public static void DrawRasterLayerScaleSettings(RasterLayerSetting a_rasterLayerSetting)
    {
        if (!ImGui.CollapsingHeader("Scale", ImGuiTreeNodeFlags.DefaultOpen))
            return;

        ImGui.PushID("RasterScale");
        RasterScale currentScale = a_rasterLayerSetting.rasterScale;
        ImGui.InputInt("Min", ref currentScale.minValue);
        ImGui.InputInt("Max", ref currentScale.maxValue);
        if (ImGui.BeginCombo("Interpolation Type", currentScale.interpolation.ToString()))
        {
            for (int enumIndex = 0; enumIndex < (int)RasterScale.EInterpolation.Count; enumIndex++)
            {
                RasterScale.EInterpolation currentInterpolation = (RasterScale.EInterpolation)enumIndex;
                if (ImGui.Selectable(currentInterpolation.ToString()))
                {
                    currentScale.interpolation = currentInterpolation;
                }
            }

            ImGui.EndCombo();
        }

        if (currentScale.interpolation == RasterScale.EInterpolation.LinGrouped)
        {
            ImGui.SeparatorText("Linear Scale Groups");
            if (ImGui.SmallButton("+ Add"))
            {
                a_rasterLayerSetting.rasterScale.interpolationGroups.Add(new RasterScale.InterpolationGroup());
            }

            ImGui.Indent();
            for (int i = a_rasterLayerSetting.rasterScale.interpolationGroups.Count - 1; i >= 0; i--)
            {
                ImGui.PushID(i);
                RasterScale.InterpolationGroup currentGroup = a_rasterLayerSetting.rasterScale.interpolationGroups[i];
                if (ImGui.SmallButton("-"))
                {
                    a_rasterLayerSetting.rasterScale.interpolationGroups.RemoveAt(i);
                    ImGui.PopID();
                    continue;
                }

                ImGui.SameLine();
                ImGui.InputDouble("Normalised Input Value", ref currentGroup.normalisedInputValue);
                ImGui.InputInt("Min Output Value", ref currentGroup.minOutputValue);
                ImGui.PopID();
            }
            ImGui.Unindent();
        }

        ImGui.PopID();
    }

    public static void DrawRasterTags(RasterLayerSetting a_rasterLayerSetting)
    {
        DrawDynamicStringList("Tags", a_rasterLayerSetting.tags);
    }

    // ---------------------------------------------------------------------
    // Colour helpers
    // ---------------------------------------------------------------------
    private static void DrawColorSwatch(string a_hex)
    {
        AugGisConfigUtils.TryParseHexColor(a_hex, out uint value);
        Vector3 color = AugGisConfigUtils.HexToVec3(value);
        ImGui.ColorButton("##swatch", new Vector4(color, 1f),
            ImGuiColorEditFlags.NoTooltip | ImGuiColorEditFlags.NoPicker, new System.Numerics.Vector2(16, 16));
        ImGui.SameLine();
    }

    public static void DrawColorInputFromHexString(string a_label, ref string a_hexString)
    {
        AugGisConfigUtils.TryParseHexColor(a_hexString, out uint hexValue);
        Vector3 color = AugGisConfigUtils.HexToVec3(hexValue);

        if (ImGui.ColorEdit3(a_label, ref color))
        {
            a_hexString = "#" + AugGisConfigUtils.Vec3ToHex(color).ToString("X6");
        }
    }

    // ---------------------------------------------------------------------
    // Path browser (used by the Launch Server popup)
    // ---------------------------------------------------------------------
    public static unsafe void DrawPathBrowser(SDLWindow* a_window, OpenFileDialogHandle a_openFileDialogHandle, string a_label = "Path")
    {
        ImGui.InputText(a_label, ref a_openFileDialogHandle.pickedPath, 512);
        ImGui.SameLine();

        if (ImGui.Button("..."))
        {
            ShowOpenFileDialog(a_window, a_openFileDialogHandle);
        }
    }
}