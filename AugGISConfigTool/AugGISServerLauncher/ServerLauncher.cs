using System.Diagnostics;

namespace AugGISServerLauncher;

public static class ServerLauncher
{
	public static void LaunchServer(string a_serverExePath, string a_configZipPath)
	{
		try
		{
			if (!File.Exists(a_serverExePath))
			{
				Console.WriteLine($"AugGISServerLauncher Error: Server build not found at '{a_serverExePath}'");
				return;
			}

			if (!File.Exists(a_configZipPath))
			{
				Console.WriteLine($"AugGISServerLauncher Error: config zip file not found at '{a_configZipPath}'");
				return;
			}

			ProcessStartInfo startInfo = new ProcessStartInfo
			{
				FileName = a_serverExePath,
				Arguments = $"configUrl={a_configZipPath}",
				UseShellExecute = true,
				WorkingDirectory = Path.GetDirectoryName(a_serverExePath)
			};

			Process? process = Process.Start(startInfo);

			if (process != null)
			{
				Console.WriteLine($"AugGIS Server Launched '{a_serverExePath}' with config: `{a_configZipPath}`");
			}
			else
			{
				Console.WriteLine("Failed to launch AugGIS Server.");
			}
		}
		catch (Exception e)
		{
			Console.WriteLine($"Failed to launch AugGIS Server. Error: {e.ToString()}");
			throw;
		}
	}
}