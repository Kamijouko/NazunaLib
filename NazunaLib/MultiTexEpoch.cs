﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace NareisLib
{
    public class MultiTexEpoch : IExposable
    {
        //每个Epoch对应一个需要多层渲染的物体

        //原物体def
        public string originalDef = "";

        //之所以用list是因为序列化list比dictionary方便
        public List<MultiTexBatch> batches = new List<MultiTexBatch>();
        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref originalDef, "originalDef", "", false);
            Scribe_Collections.Look<MultiTexBatch>(ref batches, "batches", LookMode.Deep, Array.Empty<object>());
        }
    }
}
