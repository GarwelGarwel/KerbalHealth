namespace KerbalHealth
{
    public class TrainingPart : IConfigNode
    {
        public const string ConfigNodeName = "TRAINING_PART";

        public string Name { get; set; }
        
        public double Complexity { get; set; }

        public double Level { get; set; }

        public string Label => PartLoader.getPartInfoByName(Name)?.title ?? Name;

        public bool OnCurrentVessel => Complexity > 0;

        public bool TrainingComplete => Level >= Core.TrainingCap;

        public bool TrainingNow => Complexity > 0 && Level < Core.TrainingCap;

        public TrainingPart(ConfigNode n) => Load(n);

        public TrainingPart(string name, double complexity = 1, double level = 0)
        {
            Complexity = complexity;
            Name = name;
            Level = level;
        }

        public void StartTraining(double complexity) => Complexity = complexity;

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
            Core.Log($"Loading training part from ConfigNode: {node}");
            Name = node.GetString("name");
            if (Name == null)
            {
                Core.Log("Training part name not found.", LogLevel.Error);
                return;
            }
            Complexity = node.GetDouble("complexity");
            Level = node.GetDouble("level");
        }
    }
}
