using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KerbalHealth
{
    public class TrainingPart
    {
        public uint Id { get; set; }
        public string Name { get; set; }
        public double Complexity { get; set; } = 1;
        public double TrainingLevel { get; set; } = 0;

        public ConfigNode ConfigNode
        {
            get
            {
                ConfigNode n = new ConfigNode("TRAINED_PART");
                n.AddValue("id", Id);
                n.AddValue("name", Name);
                n.AddValue("complexity", Complexity);
                n.AddValue("trainingLevel", TrainingLevel);
                return n;
            }
            set
            {
                Id = Core.GetUInt(value, "id");
                if (Id == 0)
                {
                    Core.Log("Incorrect part id 0 for training part.", Core.LogLevel.Error);
                    return;
                }
                Name = Core.GetString(value, "name", "");
                Complexity = Core.GetDouble(value, "complexity", 1);
                TrainingLevel = Core.GetDouble(value, "trainingLevel");
            }
        }

        public TrainingPart(ConfigNode n) => ConfigNode = n;

        public TrainingPart(uint id, string name, double complexity = 1, double trainingLevel = 0)
        {
            Id = id;
            Complexity = complexity;
            Name = name;
            TrainingLevel = trainingLevel;
        }
    }
}
