using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCodeInfo
{
    class GCodeModel
    {
        public string Slicer = "Unknown";
        public float PrintTime = 0;
        public float TotalFilament = 0;
        public float Width = 0;
        public float Height = 0;
        public float Depth = 0;
        public int Layers = 0;

        public List<List<GCodeCommand>> Commands = new List<List<GCodeCommand>>();
    }
}
