namespace KerbalHealth
{
    public class PartTrainingInfo : IConfigNode
    {
        public const string ConfigNodeName = "TRAINING_PART";

        public string Name { get; set; }
        
        public float Complexity { get; set; }

        public float Level { get; set; }

        public string Label => PartLoader.getPartInfoByName(Name)?.title ?? Name;

        public bool KSCTrainingComplete => Level >= Core.TrainingCap;

        public bool TrainingNow => Complexity > 0 && Level < Core.TrainingCap;

        public PartTrainingInfo(ConfigNode n) => Load(n);

        public PartTrainingInfo(string name, float complexity, float level = 0)
        {
            Name = name;
            Complexity = complexity;
            Level = level;
        }

        public void StartTraining(float complexity) => Complexity += complexity;

        public void StopTraining() => Complexity = 0;

        public void Save(ConfigNode node)
        {
            Core.Log($"Saving training part {Name}, complexity = {Complexity}, level = {Level:P2}.");
            node.AddValue("name", Name);
            if (Complexity > 0)
                node.AddValue("complexity", Complexity);
            node.AddValue("level", Level);
        }

        public void Load(ConfigNode node)
        {
            Name = node.GetString("name");
            if (Name == null)
            {
                Core.Log("Training part name not found.", LogLevel.Error);
                return;
            }
            Complexity = node.GetFloat("complexity");
            Level = node.GetFloat("level");
        }
    }
}
