using System.Drawing;
using System.Globalization;
using System.Numerics;
using AugGISDataParser;
using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using Hexa.NET.Utilities;

namespace AugGISConfigToolGUI;

public class OpenFileDialogHandle
{
	public bool hasFinished = false;
	public string? pickedPath = string.Empty;
}

public static class ImGuiAugGisDrawer
{
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
		}
		
		ImGui.EndPopup();
	}

	public static unsafe void ShowOpenFolderDialog(SDLWindow* a_window, OpenFileDialogHandle a_handle)
	{
		SDL.ShowOpenFolderDialog(((a_userdata, a_fileList, a_filter) =>
		{
			a_handle.hasFinished = true;
			a_handle.pickedPath = Utils.ToStringFromUTF8(a_fileList[0]);
		}), null, a_window, "", false);
	}
	
	public static unsafe void ShowOpenFileDialog(SDLWindow* a_window, OpenFileDialogHandle a_handle)
	{
		SDL.ShowOpenFileDialog((a_userdata, a_fileList, a_filter) =>
		{
			a_handle.hasFinished = true;
			a_handle.pickedPath = Utils.ToStringFromUTF8(a_fileList[0]);
		},null, a_window, null, 0,"", false );
	}
	
	public static void DrawSettingsDataModel(SettingsDataModel a_settingsDataModel)
	{
		ImGui.InputText("Region", ref a_settingsDataModel.region, 100);

		ImGui.PushItemWidth(100);
		ImGui.InputDouble("Min X", ref a_settingsDataModel.coordinate0.x);
		ImGui.SameLine();
		ImGui.InputDouble("Min Y", ref a_settingsDataModel.coordinate0.y);

		ImGui.InputDouble("Max X", ref a_settingsDataModel.coordinate1.x);
		ImGui.SameLine();
		ImGui.InputDouble("Max Y", ref a_settingsDataModel.coordinate1.y);
		ImGui.PopItemWidth();

        ImGui.Checkbox("Generate Basemap", ref a_settingsDataModel.generateBasemap);

        if (ImGui.TreeNode("Vector Layer Settings"))
		{
			for (int i = 0; i < a_settingsDataModel.vectorLayerSettings.Count; i++)
			{
				VectorLayerSetting vectorLayerSetting = a_settingsDataModel.vectorLayerSettings[i];
				DrawVectorLayerSettings(vectorLayerSetting, i);
			}

			ImGui.TreePop();
		}

		if (ImGui.TreeNode("Raster Layer Settings"))
		{
			for (int i = 0; i < a_settingsDataModel.rasterLayerSettings.Count; i++)
			{
				RasterLayerSetting rasterLayerSetting = a_settingsDataModel.rasterLayerSettings[i];
				DrawRasterLayerSettings(rasterLayerSetting, i);
			}

			ImGui.TreePop();
		}
	}

	public static void DrawVectorLayerSettings(VectorLayerSetting a_vectorLayerSetting, int a_id)
	{
		ImGui.PushID(a_id);
		if (ImGui.TreeNode(a_id.ToString(), a_vectorLayerSetting.name))
		{
			DrawVectorLayerTypeSelection(a_vectorLayerSetting);
			DrawDynamicStringList("Tags",a_vectorLayerSetting.tags);
			DrawLayerTypesForVectorLayerSettings(a_vectorLayerSetting);
			ImGui.TreePop();
		}

		ImGui.PopID();
	}

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
							a_vectorLayerSetting.layerTypeData.Add(new LayerTypeData(){name = attribValue.ToString()});
						}
					}
					a_vectorLayerSetting.selectedTypeKey = key;
				}
			}

			ImGui.EndCombo();
		}
	}

	public static void DrawRasterLayerSettings(RasterLayerSetting a_rasterLayerSetting, int a_id)
	{
		ImGui.PushID(a_id);
		if (ImGui.TreeNode(a_id.ToString(), a_rasterLayerSetting.name))
		{
			ImGui.PushID("TypeSettings");
			DrawRasterLayerTypeSettings(a_rasterLayerSetting);
			ImGui.PopID();

			ImGui.PushID("TagsSettings");
			DrawRasterTags(a_rasterLayerSetting);
			ImGui.PopID();
			
			ImGui.PushID("MappingSettings");
			DrawRasterLayerMappingSettings(a_rasterLayerSetting);
			ImGui.PopID();

			ImGui.PushID("ScaleSettings");
			DrawRasterLayerScaleSettings(a_rasterLayerSetting);
			ImGui.PopID();

			ImGui.TreePop();
		}

		ImGui.PopID();
	}

	public static void DrawDynamicStringList(string a_label, List<string> a_stringList)
	{
		if (ImGui.Button("+"))
		{
			a_stringList.Add(string.Empty);
		}

		ImGui.SameLine();
		if (ImGui.TreeNode(a_label))
		{
			for (int i = a_stringList.Count - 1; i >= 0; i--)
			{
				ImGui.PushID(i);
				if (ImGui.Button("-"))
				{
					a_stringList.RemoveAt(i);
					ImGui.PopID();
					continue;
				}

				ImGui.SameLine();
				string currentString = a_stringList[i];
				ImGui.InputText("Type", ref currentString, (nuint)255);
				a_stringList[i] = currentString;
				ImGui.PopID();
			}

			ImGui.TreePop();
		}
	}
	
	public static void DrawRasterLayerTypeSettings(RasterLayerSetting a_rasterLayerSetting)
	{
		if (ImGui.Button("+"))
		{
			a_rasterLayerSetting.rasterLayerTypes.Add(new LayerTypeData());
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

	public static void DrawRasterLayerMappingSettings(RasterLayerSetting a_rasterLayerSetting)
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
					ImGui.InputInt("Min", ref currentMapping.min);
					ImGui.InputInt("Max", ref currentMapping.max);
					string previewName = a_rasterLayerSetting.rasterLayerTypes.Count == 0
						? "##"
						: a_rasterLayerSetting.rasterLayerTypes[currentMapping.typeIndex].name;
					if (ImGui.BeginCombo("Choose Type", previewName))
					{
						foreach (LayerTypeData type in a_rasterLayerSetting.rasterLayerTypes)
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

	public static void DrawRasterLayerScaleSettings(RasterLayerSetting a_rasterLayerSetting)
	{
		if (ImGui.TreeNode("Scale"))
		{
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
						currentScale.interpolation = (RasterScale.EInterpolation)enumIndex;
					}
				}

				ImGui.EndCombo();
			}

			if (currentScale.interpolation == RasterScale.EInterpolation.LinGrouped)
			{
				if (ImGui.Button("+"))
				{
					a_rasterLayerSetting.rasterScale.interpolationGroups.Add(new RasterScale.InterpolationGroup());
				}

				ImGui.SameLine();
				if (ImGui.TreeNode("Linear Scale Groups"))
				{
					for (int i = a_rasterLayerSetting.rasterScale.interpolationGroups.Count - 1; i >= 0; i--)
					{
						ImGui.PushID(i);
						if (ImGui.Button("-"))
						{
							a_rasterLayerSetting.rasterScale.interpolationGroups.RemoveAt(i);
							ImGui.PopID();
							continue;
						}

						ImGui.SameLine();
						if (ImGui.TreeNode("Group"))
						{
							RasterScale.InterpolationGroup currentGroup =
								a_rasterLayerSetting.rasterScale.interpolationGroups[i];
							ImGui.InputDouble("Normalised Input Value", ref currentGroup.normalisedInputValue);
							ImGui.InputInt("Min Output Value", ref currentGroup.minOutputValue);

							ImGui.TreePop();
						}

						ImGui.PopID();
					}

					ImGui.TreePop();
				}
			}

			ImGui.TreePop();
		}
	}

	public static void DrawRasterTags(RasterLayerSetting a_rasterLayerSetting)
	{
		DrawDynamicStringList("Tags",a_rasterLayerSetting.tags);
	}

	public static void DrawLayerTypesForVectorLayerSettings(VectorLayerSetting a_vectorLayerSetting)
	{
		if(ImGui.TreeNode("Type Data Settings"))
		{
			ImGui.PushID("Type Data Settings");
			for(int i = 0; i< a_vectorLayerSetting.layerTypeData.Count; i++)
			{
				LayerTypeData  layerTypeData = a_vectorLayerSetting.layerTypeData[i]; 
				ImGui.PushID(i);
				if (ImGui.TreeNode(layerTypeData.name))
				{
					DrawLayerTypeData(layerTypeData);
					ImGui.TreePop();
				}
				ImGui.PopID();
			}
			
			ImGui.PopID();
			ImGui.TreePop();
		}
	}
	
	public static void DrawLayerTypeData(LayerTypeData a_layerTypeData)
	{
		ImGui.TextDisabled(a_layerTypeData.name);
		ImGui.InputInt("Value", ref a_layerTypeData.value);
		
		DrawColorInputFromHexString("Polygon Color", ref a_layerTypeData.polygonColor);
		ImGui.InputText("Polygon Pattern Name", ref a_layerTypeData.polygonPatternName, (nuint)255);
		
		DrawColorInputFromHexString("Line Color", ref a_layerTypeData.lineColor);
		ImGui.InputFloat("Line Width", ref a_layerTypeData.lineWidth);
		ImGui.InputText("Line Icon", ref a_layerTypeData.lineIcon, (nuint)255);
		ImGui.InputText("Line Pattern Type", ref a_layerTypeData.linePatternType, (nuint)255);
		
		DrawColorInputFromHexString("Point Color", ref a_layerTypeData.pointColor);
		ImGui.InputFloat("Point Size", ref a_layerTypeData.pointSize);
		ImGui.InputText("Point Sprite Name", ref a_layerTypeData.pointSpriteName, (nuint)255);
		
		ImGui.InputText("Description", ref a_layerTypeData.description, (nuint)255);
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

	public static unsafe void DrawPathBrowser(SDLWindow * a_window, OpenFileDialogHandle a_openFileDialogHandle, string a_label = "Path")
	{
		ImGui.InputText(a_label, ref a_openFileDialogHandle.pickedPath, 512);
		ImGui.SameLine();

		if (ImGui.Button("..."))
		{
			ShowOpenFileDialog(a_window, a_openFileDialogHandle);
		}
	}
}