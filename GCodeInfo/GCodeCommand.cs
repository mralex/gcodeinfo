using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCodeInfo
{
    struct GCodeCommand
    {
        public float X;
        public float Y;
        public float Z;
        public bool Extrude;
        public float Retract;
        public bool NoMove;
        public float Extrusion;
        public string Extruder;
        public float PreviousX;
        public float PreviousY;
        public float PreviousZ;
        public float Speed;
        public int GCodeLine;
        public float VolumePerMM;
    }
}
