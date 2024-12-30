﻿using Shared.Core.ByteParsing;

namespace Shared.GameFormats.Wwise.Hirc
{
    public class HircParser
    {
        public HircParser()
        {
        }

        private static HircFactory GetHircFactory(uint bnkVersion)
        {
            return HircFactory.CreateFactory(bnkVersion);
        }

        public HircChunk Parse(string fileName, ByteChunk chunk, uint bnkVersion, bool isCaHircItem)
        {
            var hircChunk = new HircChunk
            {
                ChunkHeader = BnkChunkHeader.CreateSpecificData(chunk),
                NumHircItems = chunk.ReadUInt32()
            };

            var failedItems = new List<uint>();
            var factory = GetHircFactory(bnkVersion);

            for (uint itemIndex = 0; itemIndex < hircChunk.NumHircItems; itemIndex++)
            {
                var hircType = (HircType)chunk.PeakByte();

                var start = chunk.Index;
                try
                {
                    var hircItem = factory.CreateInstance(hircType);
                    hircItem.IndexInFile = itemIndex;
                    hircItem.ByteIndexInFile = itemIndex;
                    hircItem.OwnerFile = fileName;
                    hircItem.IsCaHircItem = isCaHircItem;
                    hircItem.Parse(chunk);
                    hircChunk.HircItems.Add(hircItem);
                }
                catch (Exception e)
                {
                    failedItems.Add(itemIndex);
                    chunk.Index = start;

                    var unkInstance = new CAkUnknown() { ErrorMsg = e.Message, ByteIndexInFile = itemIndex, OwnerFile = fileName };
                    unkInstance.Parse(chunk);
                    hircChunk.HircItems.Add(unkInstance);
                }
            }

            return hircChunk;
        }

        public byte[] GetAsBytes(HircChunk hircChunk)
        {
            using var memStream = new MemoryStream();
            memStream.Write(BnkChunkHeader.GetAsByteArray(hircChunk.ChunkHeader));
            memStream.Write(ByteParsers.UInt32.EncodeValue(hircChunk.NumHircItems, out _));

            foreach (var hircItem in hircChunk.HircItems)
            {
                var bytes = hircItem.GetAsByteArray();
                memStream.Write(bytes);
            }

            var byteArray = memStream.ToArray();

            // For sanity, read back
            Parse("name", new ByteChunk(byteArray), 136, true);

            return byteArray;
        }
    }
}
