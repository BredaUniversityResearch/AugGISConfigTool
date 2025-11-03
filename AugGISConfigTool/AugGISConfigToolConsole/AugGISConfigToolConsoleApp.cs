internal class AugGISConfigToolConsoleApp
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("[AugGISConfigToolConsoleApp] [Error] Please provide file.");
            return;
        }

        string gisFilePath = args[0];
        
        AugGISDataParser.SettingsDataModel dataModel = AugGISDataParser.SettingsDataCreator.CreateSettingsDataModelFromGISData(gisFilePath);
        AugGISDataParser.SettingsDataCreator.SaveSettingsDataModelToFile(dataModel);
    }
}