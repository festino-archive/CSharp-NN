using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using YOLOv4MLNet.DataStructures;

namespace Lab
{
    class RecognisionResult
    {
        public string Filename;
        public List<DetectedObject> Objects;

        public RecognisionResult(string filename, List<DetectedObject> objects)
        {
            Filename = filename;
            Objects = objects;
        }
    }

    class DetectedObject
    {
        public string Label;
        public Int32Rect Box;

        public DetectedObject(YoloV4Result res)
        {
            Label = res.Label;
            int x1 = (int)Math.Floor(res.BBox[0]);
            int y1 = (int)Math.Floor(res.BBox[1]);
            int x2 = (int)Math.Floor(res.BBox[2]);
            int y2 = (int)Math.Floor(res.BBox[3]);
            Box = new Int32Rect(x1, y1, x2 - x1, y2 - y1);
        }
    }
}
