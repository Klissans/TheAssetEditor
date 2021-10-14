﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Filetypes.RigidModel.LodHeader
{
    public struct Rmv2LodHeader_V7 : RmvLodHeader
    {
        uint _meshCount;
        uint _totalLodVertexSize;
        uint _totalLodIndexSize;
        uint _firstMeshOffset;
        float _lodCameraDistance;
        public uint _lodLevel;
        public byte _qualityLvl;
        public byte _padding0;
        public byte _padding1;
        public byte _padding2;

        public uint MeshCount { get => _meshCount; set => _meshCount = value; }
        public uint TotalLodVertexSize { get => _totalLodVertexSize; set => _totalLodVertexSize = value; }
        public uint TotalLodIndexSize { get => _totalLodIndexSize; set => _totalLodIndexSize = value; }
        public uint FirstMeshOffset { get => _firstMeshOffset; set => _firstMeshOffset = value; }

        public byte QualityLvl { get => _qualityLvl; set => _qualityLvl = value; }
        public float LodCameraDistance { get => _lodCameraDistance; set => _lodCameraDistance = value; }

        public int GetHeaderSize() => ByteHelper.GetSize(typeof(Rmv2LodHeader_V7));

        public RmvLodHeader Clone()
        {
            return new Rmv2LodHeader_V7()
            {
                _meshCount = MeshCount,
                _totalLodVertexSize = _totalLodVertexSize,
                _totalLodIndexSize = _totalLodIndexSize,
                _firstMeshOffset = _firstMeshOffset,
                _lodCameraDistance = _lodCameraDistance,

                _lodLevel = _lodLevel,
                _qualityLvl = _qualityLvl,
                _padding0 = _padding0,
                _padding1 = _padding1,
                _padding2 = _padding2,
            };
        }
    }
}
