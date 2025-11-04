using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using AugGISDataParser;

internal class AugGISConfigToolConsoleApp
{
	static int Main(string[] args)
	{
		RootCommand rootCommand = new RootCommand("AugGIS Config Tool Console");
		
		Command createSettingsCommand = new Command("create_settings", "Create settings file from GIS data");
		Option<string> settingInputOption = new Option<string>("--input", "-i")
		{
			Description = "Input GIS Data Directory Path",
			Required = true
		};
		
		Option<string> settingOutputOption = new Option<string>("--output", "-o")
		{
			Description = "Output Settings file path",
			Required = true
		};
		
		createSettingsCommand.Add(settingInputOption);
		createSettingsCommand.Add(settingOutputOption);
		createSettingsCommand.SetAction((ParseResult a_result) =>
		{
			string? gisFileDirectoryPath = a_result.GetValue<string>(settingInputOption);
			string? outputSettingsPath =  a_result.GetValue<string>(settingOutputOption);
			
			AugGISDataParser.SettingsDataModel dataModel = AugGISDataParser.SettingsDataCreator.CreateSettingsDataModelFromGISData(gisFileDirectoryPath);
			AugGISDataParser.SettingsDataCreator.SaveSettingsDataModelToFile(dataModel,outputSettingsPath);
		});
		
		rootCommand.Add(createSettingsCommand);
		
		Command createConfigCommand = new Command("create_config", "Create config file from settings");
		Option<string> configInputOption = new Option<string>("--input", "-i")
		{
			Description = "Input Settings file path",
			Required = true
		};
		
		Option<string> configOutputOption = new Option<string>("--output", "-o")
		{
			Description = "Output Config file path",
			Required = true
		};
		
		createConfigCommand.Add(configInputOption);
		createConfigCommand.Add(configOutputOption);
		
		createConfigCommand.SetAction((ParseResult a_result) =>
		{
			string? settingsFilePath = a_result.GetValue<string>(configInputOption);
			string? outputConfigFilePath = a_result.GetValue<string>(configOutputOption);

			SettingsDataModel settingsDataModel =  AugGISDataParser.SettingsDataCreator.LoadSettingsDataModelFromFile(settingsFilePath);
			JsonConfigObject configObject = AugGISDataParser.ConfigDataCreator.CreateConfigDataModelFromSettings(settingsDataModel);
			AugGISDataParser.ConfigDataCreator.SaveConfigObjectToFile(configObject, outputConfigFilePath);
		});
		
		rootCommand.Add(createConfigCommand);

		ParseResult parseResult;
		if (args.Length == 0)
		{
			string userInput = Console.ReadLine();
			parseResult = rootCommand.Parse(userInput);
		}
		else
		{
			parseResult = rootCommand.Parse(args);
		}
		
		return parseResult.Invoke();
	}
}