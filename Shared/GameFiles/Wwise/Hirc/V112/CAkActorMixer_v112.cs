﻿using Shared.Core.ByteParsing;
using Shared.GameFormats.Wwise.Hirc.V112.Shared;

namespace Shared.GameFormats.Wwise.Hirc.V112
{
    public class CAkActorMixer_v112 : HircItem, ICAkActorMixer
    {
        public NodeBaseParams_V112 NodeBaseParams { get; set; } = new NodeBaseParams_V112();
        public Children_V112 Children { get; set; } = new Children_V112();

        protected override void CreateSpecificData(ByteChunk chunk)
        {
            NodeBaseParams.Create(chunk);
            Children.Create(chunk);
        }

        public override byte[] GetAsByteArray()
        {
            using var memStream = WriteHeader();
            memStream.Write(NodeBaseParams.GetAsByteArray());
            memStream.Write(Children.GetAsByteArray());
            var byteArray = memStream.ToArray();

            // Reload the object to ensure sanity
            var sanityReload = new CAkActorMixer_v112();
            sanityReload.Parse(new ByteChunk(byteArray));

            return byteArray;
        }

        public override void UpdateSectionSize()
        {
            var idSize = ByteHelper.GetPropertyTypeSize(Id);
            SectionSize = idSize + Children.GetSize() + NodeBaseParams.GetSize();
        }

        public List<uint> GetChildren() => Children.ChildIds;
        public uint GetDirectParentId() => NodeBaseParams.DirectParentId;
    }
}
