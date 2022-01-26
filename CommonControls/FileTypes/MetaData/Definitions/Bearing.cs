﻿using System;
using System.Collections.Generic;
using System.Text;

namespace CommonControls.FileTypes.MetaData.Definitions
{
    [MetaData("BEARING", 10)]
    public class Bearing_v10 : DecodedMetaEntryBase
    {
        [MetaDataTag(5)]
        public float Unk { get; set; }
    }
}
