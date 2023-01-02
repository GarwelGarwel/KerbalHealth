using System;

namespace KerbalHealth
{
    public class PartTrainingInfo : IConfigNode
    {
        public const string ConfigNodeName = "TRAINING_PART";

        public string Name { get; set; }
        
        public float Complexity { get; set; }

        public int Count { get; set; }

        public float Level { get; set; }

        public string Title => Core.GetPartTitle(Name);

        public bool KSCTrainingComplete => Level >= Core.KSCTrainingCap;

        public bool TrainingNow => Complexity > 0;

        public PartTrainingInfo(ConfigNode n) => Load(n);

        public PartTrainingInfo(string name, float complexity, int count, float level = 0)
        {
            Name = name;
            Complexity = complexity;
            Count = count;
            Level = level;
        }

        public void StartTraining(float complexity) => Count++;

        public void StopTraining() => Count = 0;

        public void Save(ConfigNode node)
        {
            Core.Log($"Saving training part {Name}, complexity = {Complexity:P0}, count = {Count}, level = {Level:P2}.");
            node.AddValue("name", Name);
            node.AddValue("complexity", Complexity);
            node.AddValue("count", Count);
            node.AddValue("level", Level);
        }

        public void Load(ConfigNode node)
        {
            Name = node.GetString("name");
            if (Name == null)
            {
                Core.Log($"Training part name not found in node:\n{node}", LogLevel.Error);
                return;
            }
            Complexity = node.GetFloat("complexity");
            Count = node.GetInt("count", Complexity > 0 ? 1 : 0);
            Level = node.GetFloat("level");
            if (!KerbalHealthFactorsSettings.Instance.TrainingEnabled)
                Level = Math.Max(Level, Core.KSCTrainingCap);
        }
    }
}
