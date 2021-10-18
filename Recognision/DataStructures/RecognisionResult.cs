using System;
using System.Collections.Generic;
using System.Windows;
using YOLOv4MLNet.DataStructures;

namespace Lab
{
    public class RecognisionResult
    {
        public string Filename;
        public List<DetectedObject> Objects;

        public RecognisionResult(string filename, List<DetectedObject> objects)
        {
            Filename = filename;
            Objects = objects;
        }
    }

    public class DetectedObject
    {
        public string Label;
        public int X1 { get; private set; }
        public int Y1 { get; private set; }
        public int X2 { get; private set; }
        public int Y2 { get; private set; }

        public DetectedObject(YoloV4Result res)
        {
            Label = res.Label;
            X1 = (int)Math.Floor(res.BBox[0]);
            Y1 = (int)Math.Floor(res.BBox[1]);
            X2 = (int)Math.Floor(res.BBox[2]);
            Y2 = (int)Math.Floor(res.BBox[3]);
        }
    }
}
