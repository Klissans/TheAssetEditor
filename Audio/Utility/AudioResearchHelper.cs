﻿using Audio.FileFormats.WWise.Hirc.V136;
using Audio.Storage;
using CommonControls.Common;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Audio.Utility
{
    public class AudioResearchHelper
    {
        ILogger _logger = Logging.Create<AudioResearchHelper>();
        private readonly IAudioRepository _audioRepository;

        public AudioResearchHelper(IAudioRepository audioRepository)
        {
            _audioRepository = audioRepository;
        }

        public void ExportNamesToFile(string path, bool openFile = false)
        {
            var stringbuilder = new StringBuilder();
            foreach (var item in _audioRepository.NameLookUpTable)
                stringbuilder.AppendLine($"{item.Key}, {item.Value}");

            File.WriteAllText(path, stringbuilder.ToString());

            Process.Start("explorer.exe", "c:\\temp");
        }

        public void ExportDialogEventsToFile(CAkDialogueEvent_v136 dialogEvent, bool openFile = false)
        {
            if (dialogEvent == null)
            {
                _logger.Here().Warning("Error Converting to CSV - DialogEvent not correct version");
                return;
            }

            var numArgs = dialogEvent.ArgumentList.Arguments.Count() - 1;
            var root = dialogEvent.AkDecisionTree.Root.Children.First();
            if (numArgs == 0)
                throw new Exception("Error Converting to CSV - No arguments in DialogEvent");

            var table = new List<string>();
            foreach (var children in root.Children)
                GenerateRow(children, 0, numArgs, new Stack<string>(), table, _audioRepository);

            var keys = dialogEvent.ArgumentList.Arguments.Select(x => _audioRepository.GetNameFromHash(x.ulGroupId)).ToList();
            var prettyKeys = string.Join("|", keys);
            prettyKeys += "|WWiseChild";
            var prettyTable = table.Select(x => string.Join("|", x)).ToList();

            var ss = new StringBuilder();
            ss.AppendLine("sep=|");
            ss.AppendLine(prettyKeys);
            foreach (var row in prettyTable)
                ss.AppendLine(row);

            var wholeFile = ss.ToString();


            /*
             
                         var text = ExportDialogEventsToFile(dialogEvent);
            var name = _nameLookUpHelper.GetName(dialogEvent.Id);
            var folderPath = "c:\\temp\\wwiseDialogEvents";
            var filePath = $"{folderPath}\\{name}.csv";
            DirectoryHelper.EnsureCreated(folderPath);
            File.WriteAllText(filePath, text.ToString());
            DirectoryHelper.OpenFolderAndSelectFile(filePath);
             */
        }

        static void GenerateRow(AkDecisionTree.Node currentNode, int currentArgrument, int numArguments, Stack<string> pushList, List<string> outputList, IAudioRepository audioRepository)
        {
            var currentNodeContent = audioRepository.GetNameFromHash(currentNode.Key);
            pushList.Push(currentNodeContent);

            bool isDone = numArguments == currentArgrument;
            if (isDone)
            {
                var currentLine = pushList.ToArray().Reverse().ToList();
                currentLine.Add(currentNode.AudioNodeId.ToString());  // Add the wwise child node
                outputList.Add(string.Join("|", currentLine));
            }
            else
            {
                foreach (var child in currentNode.Children)
                    GenerateRow(child, currentArgrument + 1, numArguments, pushList, outputList, audioRepository);
            }

            pushList.Pop();
        }
    }
}
