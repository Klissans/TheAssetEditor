﻿using CommunityToolkit.Diagnostics;
using System;
using System.Linq;
using Shared.GameFormats.Wwise;
using Shared.GameFormats.Wwise.Hirc.V136;
using System.Collections.Generic;
using Editors.Audio.BnkCompiler.ObjectConfiguration.Warhammer3;
using Shared.GameFormats.Wwise.Hirc;

namespace Editors.Audio.BnkCompiler.ObjectGeneration.Warhammer3
{
    public class RandomContainerGenerator : IWwiseHircGenerator
    {
        public string GameName => CompilerConstants.GameWarhammer3;
        public Type AudioProjectType => typeof(RandomContainer);

        public HircItem ConvertToWwise(IAudioProjectHircItem projectItem, CompilerData project)
        {
            var typedProjectItem = projectItem as RandomContainer;
            Guard.IsNotNull(typedProjectItem);
            return ConvertToWwise(typedProjectItem, project);
        }

        public static CAkRanSeqCntr_v136 ConvertToWwise(RandomContainer inputContainer, CompilerData project)
        {
            var wwiseRandomContainer = new CAkRanSeqCntr_v136();
            wwiseRandomContainer.Id = inputContainer.Id;
            wwiseRandomContainer.Type = HircType.SequenceContainer;
            wwiseRandomContainer.NodeBaseParams = NodeBaseParams.CreateDefaultRandomContainer();
            wwiseRandomContainer.ByBitVector = 0x12;
            wwiseRandomContainer.FTransitionTime = 1000;
            wwiseRandomContainer.NodeBaseParams.DirectParentId = inputContainer.DirectParentId;
            wwiseRandomContainer.SLoopCount = 1;
            wwiseRandomContainer.WAvoidRepeatCount = 2;

            var allChildIds = inputContainer.Children.Select(x => x).OrderBy(x => x).ToList();
            wwiseRandomContainer.Children = CreateChildrenList(allChildIds);
            wwiseRandomContainer.AkPlaylist = allChildIds.Select(CreateAkPlaylistItem).ToList();

            wwiseRandomContainer.UpdateSize();

            return wwiseRandomContainer;
        }

        private static AkPlaylistItem CreateAkPlaylistItem(uint childId)
        {
            return new AkPlaylistItem
            {
                PlayId = childId,
                Weight = 50000
            };
        }

        private static Children CreateChildrenList(List<uint> childIds)
        {
            return new Children
            {
                ChildIdList = childIds
            };
        }
    }
}
