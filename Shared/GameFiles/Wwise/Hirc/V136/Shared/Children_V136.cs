﻿using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc.V136.Shared
{
    public class Children_V136
    {
        public uint NumChilds { get; set; }
        public List<uint> ChildIds { get; set; } = [];

        public void Create(ByteChunk chunk)
        {
            NumChilds = chunk.ReadUInt32();
            for (var i = 0; i < NumChilds; i++)
            {
                var childId = chunk.ReadUInt32();
                ChildIds.Add(childId);
            }
        }

        internal byte[] GetAsByteArray()
        {
            using var memStream = new MemoryStream();
            memStream.Write(ByteParsers.UInt32.EncodeValue((uint)ChildIds.Count, out _));
            foreach (var childId in ChildIds)
                memStream.Write(ByteParsers.UInt32.EncodeValue(childId, out _));
            return memStream.ToArray();
        }

        public uint GetSize()
        {
            var numChildsSize = ByteHelper.GetPropertyTypeSize(NumChilds);
            var childIdSize = ByteHelper.GetPropertyTypeSize(ChildIds);
            var childIdListSize = (uint)(childIdSize * ChildIds.Count);
            return numChildsSize + childIdListSize;
        }
    }
}
