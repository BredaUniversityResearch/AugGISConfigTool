using Hexa.NET.ImGui;
using Hexa.NET.SDL3;
using Hexa.NET.Utilities;

namespace AugGISConfigToolGUI;

public class OpenFileDialogHandle
{
	public bool hasFinished = false;
	public string? pickedPath = string.Empty;
}

public static class Util
{
	public static void ImGuiShowErrorPopupModal(string a_message, string a_option, Action a_onClose)
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
}