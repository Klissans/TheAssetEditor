﻿using System.IO;
using Editors.ImportExport.Exporting.Exporters.RmvToGltf;
using Shared.Core.DevConfig;
using Shared.Core.PackFiles;
using Shared.Core.PackFiles.Models;
using Shared.Core.Services;

namespace Editors.ImportExport.DevConfig
{
    internal class Export_Karl : IDeveloperConfiguration
   {
       private readonly PackFileService _packFileService;
       private readonly RmvToGltfExporter _exporter;

       public Export_Karl(PackFileService packFileService, RmvToGltfExporter exporter)
       {
           _packFileService = packFileService;
           _exporter = exporter;
       }

       public void OpenFileOnLoad()
       {
           var meshPackFile = _packFileService.FindFile(@"variantmeshes\wh_variantmodels\hu1\emp\emp_karl_franz\emp_karl_franz.rigid_model_v2");
          var animPackFile = _packFileService.FindFile(@"animations\battle\humanoid01\subset\skeleton_warriors\sword_and_shield\combat_idles\hu1_sk_sws_combat_idle_03.anim");

           // obtains user's document folder 
           var documentPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
           var destPath = $"{documentPath}\\AE_Export_Handedness\\";

           // clear folder, if it exists
           DirectoryInfo dir = new DirectoryInfo(destPath);
           if (dir.Exists)
           {
               foreach (FileInfo file in dir.GetFiles())
               {
                   file.Delete();
               }
           }

           System.IO.Directory.CreateDirectory(destPath);

           var settings = new RmvToGltfExporterSettings(meshPackFile, new List<PackFile>() { animPackFile }, null,  destPath, true, true, true);
           _exporter.Export(settings);
       }

       public void OverrideSettings(ApplicationSettings currentSettings)
       {
           currentSettings.LoadCaPacksByDefault = true;
           currentSettings.CurrentGame = GameTypeEnum.Warhammer3;            
       }
   }
}
