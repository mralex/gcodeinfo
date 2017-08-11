using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GCodeInfo
{

    /*
     * 
     * GCode Parser
     * - Open a gcode file
     * - Read each line
     * - Parse the line
     * - ???
     * - Profit!
     * 
     */

    class GCodeParser
    {
        public GCodeModel Model { get; private set; }

        private string Filename;
        private List<string> lines;

        public GCodeParser(string filename)
        {
            Model = new GCodeModel();
            Filename = filename;

            ReadFile();
        }

        public void Parse()
        {
            DoParse();
            Analyze();
        }

        private void ReadFile()
        {
            lines = new List<string>();

            System.IO.StreamReader reader = new System.IO.StreamReader(Filename);
            string line = reader.ReadLine();

            if (line == null)
            {
                // Something bad happened, this isn't really a gcode file? Empty?
                return;
            }

            if (!line.StartsWith(";"))
            {
                // Assume the file has been tinkered with, can't detect slicer...
                Model.Slicer = "Could not determine slicer";
            } else
            {
                if (line.Contains("Slic3r"))
                {
                    Model.Slicer = "Slic3r";
                }
                else if (line.Contains("KISSlicer"))
                {
                    Model.Slicer = "KISSlicer";
                }
                else if(line.Contains("skeinforge"))
                {
                    Model.Slicer = "skeinforge";
                }
                else if (line.Contains("CURA_PROFILE_STRING"))
                {
                    Model.Slicer = "Cura";
                }
                else if (line.Contains("Miracle"))
                {
                    Model.Slicer = "MakerBot";
                }
            }

            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(";") || line == "")
                {
                    continue;
                }

                lines.Add(line.Split(';')[0]);
            }
        }

        private void DoParse()
        {
            int layer = 0;
            float x = 0;
            float y = 0;
            float z = 0;
            float previousX = 0;
            float previousY = 0;
            float previousZ = 0;

            bool extruding = false;
            bool extrudeRelative = false;
            char extruder;
            string sExtruder = "";

            int retract = 0;

            float extrusion;
            Dictionary<string, float> previousExtrusion = new Dictionary<string, float>()
            {
                { "a", 0 },
                { "b", 0 },
                { "c", 0 },
                { "e", 0 },
                { "abs", 0 }
            };

            Dictionary<string, float> previousRetraction = new Dictionary<string, float>()
            {
                { "a", 0 },
                { "b", 0 },
                { "c", 0 },
                { "e", 0 },
            };

            // "Last" represents the last time the print head speed was requested to be changed
            float lastSpeed = 0;

            bool assumeStepperExtruder = true;
            float volumePerMM = 0;

            bool shouldSaveCommand = false;

            for (int i = 0; i < lines.Count; i++)
            {
                string gcode = lines[i];
                string[] args = gcode.ToLower().TrimEnd().Split(' ');

                x = float.NegativeInfinity;
                y = float.NegativeInfinity;
                z = float.NegativeInfinity;
                volumePerMM = 0;
                retract = 0;
                extruding = false;
                sExtruder = "";
                previousExtrusion["abs"] = 0;
                shouldSaveCommand = false;

                if (args[0] == "g0" || args[0] == "g1")
                {
                    for (int j = 1; j < args.Length; j++)
                    {
                        string arg = args[j];

                        switch (arg[0])
                        {
                            case 'x':
                                x = float.Parse(arg.TrimStart(arg[0]));
                                break;
                            case 'y':
                                y = float.Parse(arg.TrimStart(arg[0]));
                                break;
                            case 'z':
                                // stuff happens;
                                z = float.Parse(arg.TrimStart(arg[0]));

                                if (z == previousZ)
                                {
                                    continue;
                                }
                                else
                                {
                                    layer = Model.Commands.Count;
                                }
                                previousZ = z;

                                break;
                            case 'e':
                            case 'a':
                            case 'b':
                            case 'c':
                                // These 4 cases appear to map to different extruders
                                extruder = arg[0];
                                sExtruder = extruder.ToString();
                                extrusion = float.Parse(arg.TrimStart(arg[0]));

                                if (!extrudeRelative)
                                {
                                    // Absolute positioning
                                    previousExtrusion["abs"] = extrusion - previousExtrusion[sExtruder];
                                }
                                else
                                {
                                    previousExtrusion["abs"] = extrusion;
                                }

                                extruding = previousExtrusion["abs"] > 0;

                                if (previousExtrusion["abs"] < 0)
                                {
                                    // We're retracting...
                                    previousRetraction[sExtruder] = -1;
                                    retract = -1;
                                }
                                else if (previousExtrusion["abs"] == 0)
                                {
                                    retract = 0;
                                }
                                else if (previousExtrusion["abs"] > 0 && previousRetraction[sExtruder] < 0)
                                {
                                    previousRetraction[sExtruder] = 0;
                                    retract = 1;
                                }
                                else
                                {
                                    retract = 0;
                                }

                                previousExtrusion[sExtruder] = extrusion;

                                break;
                            case 'f':
                                lastSpeed = float.Parse(arg.TrimStart(arg[0]));
                                break;
                            default:
                                break;
                        }
                    }

                    if (extruding && retract == 0)
                    {
                        // XXX This casting is pretty gross...
                        volumePerMM = previousExtrusion["abs"] / (float)Math.Sqrt(
                            ((double)previousX - x) * ((double)previousX - x) +
                            ((double)previousY - y) * ((double)previousY - y)
                        );
                    }

                    shouldSaveCommand = true;

                    previousX = x;
                    previousY = y;
                }
                else if (args[0] == "m82")
                {
                    extrudeRelative = false;
                }
                else if (args[0] == "m83")
                {
                    extrudeRelative = true;
                }
                else if (args[0] == "g90")
                {
                    extrudeRelative = false;
                }
                else if (args[0] == "g91")
                {
                    extrudeRelative = true;
                }
                else if (args[0] == "g92")
                {
                    for (int j = 1; j < args.Length; j++)
                    {
                        string arg = args[j];

                        switch (arg[0])
                        {
                            case 'x':
                                x = float.Parse(arg.TrimStart(arg[0]));
                                break;
                            case 'y':
                                y = float.Parse(arg.TrimStart(arg[0]));
                                break;
                            case 'z':
                                // stuff happens;
                                z = float.Parse(arg.TrimStart(arg[0]));
                                previousZ = z;
                                break;
                            case 'e':
                            case 'a':
                            case 'b':
                            case 'c':
                                // These 4 cases appear to map to different extruders
                                extruder = arg[0];
                                sExtruder = extruder.ToString();
                                extrusion = float.Parse(arg.TrimStart(arg[0]));

                                if (!extrudeRelative)
                                {
                                    // Absolute positioning
                                    previousExtrusion[sExtruder] = 0;
                                }
                                else
                                {
                                    previousExtrusion[sExtruder] = extrusion;
                                }

                                break;
                            default:
                                break;
                        }
                    }
                    shouldSaveCommand = true;
                }
                else if (args[0] == "g28")
                {
                    for (int j = 1; j < args.Length; j++)
                    {
                        string arg = args[j];

                        switch (arg[0])
                        {
                            case 'x':
                                x = float.Parse(arg.TrimStart(arg[0]));
                                break;
                            case 'y':
                                y = float.Parse(arg.TrimStart(arg[0]));
                                break;
                            case 'z':
                                // stuff happens;
                                z = float.Parse(arg.TrimStart(arg[0]));

                                if (z == previousZ)
                                {
                                    continue;
                                }
                                else
                                {
                                    layer = Model.Commands.Count;
                                }
                                previousZ = z;

                                break;
                            default:
                                break;
                        }
                    }


                    shouldSaveCommand = true;
                }

                if (shouldSaveCommand)
                {
                    if (Model.Commands.Count - 1 < layer || Model.Commands[layer] == null)
                    {
                        Model.Commands.Add(new List<GCodeCommand>());
                    }

                    GCodeCommand command = new GCodeCommand();
                    command.X = x;
                    command.Y = y;
                    command.Z = z;
                    command.Speed = lastSpeed;

                    command.Extrude = extruding;
                    command.Retract = retract;
                    command.NoMove = false;
                    command.Extrusion = (extruding || retract != 0) ? previousExtrusion["abs"] : 0;
                    command.Extruder = sExtruder;

                    command.PreviousX = previousX;
                    command.PreviousY = previousY;
                    command.PreviousZ = previousZ;

                    command.VolumePerMM = volumePerMM;

                    command.GCodeLine = i;

                    Model.Commands[layer].Add(command);
                }
            }

            Model.Layers = layer;
        }

        private void Analyze()
        {
            bool validX = false;
            bool validY = false;

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;
            float minZ = float.MaxValue;
            float maxZ = float.MinValue;

            float printTimeAddition = 0;

            foreach (List<GCodeCommand> commands in Model.Commands)
            {
                if (commands.Count == 0)
                {
                    continue;
                }

                foreach(GCodeCommand command in commands)
                {
                    validX = false;
                    validY = false;

                    if (!float.IsNegativeInfinity(command.X) && !float.IsNegativeInfinity(command.PreviousX) && command.Extrude)
                    {
                        maxX = (maxX > command.X) ? maxX : command.X;
                        maxX = (maxX > command.PreviousX) ? maxX : command.PreviousX;
                        minX = (minX < command.X) ? minX : command.X;
                        minX = (minX < command.PreviousX) ? minX : command.PreviousX;

                        validX = true;
                    }

                    if (!float.IsNegativeInfinity(command.Y) && !float.IsNegativeInfinity(command.PreviousY) && command.Extrude)
                    {
                        maxY = (maxY > command.X) ? maxY : command.Y;
                        maxY = (maxY > command.PreviousY) ? maxY : command.PreviousY;
                        minY = (minY < command.X) ? minY : command.Y;
                        minY = (minY < command.PreviousY) ? minY : command.PreviousY;

                        validY = true;
                    }

                    if (!float.IsNegativeInfinity(command.PreviousZ) && command.Extrude)
                    {
                        maxZ = (maxZ > command.PreviousZ) ? maxZ : command.PreviousZ;
                        minZ = (minZ < command.PreviousZ) ? minZ : command.PreviousZ;
                    }

                    if (command.Extrude && command.Retract != 0)
                    {
                        Model.TotalFilament += command.Extrusion;
                    }
                }
            }

            Model.Width = Math.Abs(maxX - minX);
            Model.Depth = Math.Abs(maxY - minY);
            Model.Height = Math.Abs(maxZ - minZ);
        }
    }
}
