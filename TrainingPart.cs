namespace KerbalHealth
{
    public class TrainingPart : IConfigNode
    {
        public const string ConfigNodeName = "TRAINING_PART";

        public uint Id { get; set; }

        public string Name { get; set; }
        
        public double Complexity { get; set; } = 1;

        public void Save(ConfigNode node)
        {
            node.AddValue("id", Id);
            node.AddValue("name", Name);
            node.AddValue("complexity", Complexity);
        }

        public void Load(ConfigNode node)
        {
            Id = node.GetUInt("id");
            if (Id == 0)
            {
                Core.Log("Incorrect part id 0 for training part.", LogLevel.Error);
                return;
            }
            Name = node.GetString("name", "");
            Complexity = node.GetDouble("complexity", 1);
        }

        public TrainingPart(ConfigNode n) => Load(n);

        public TrainingPart(uint id, string name, double complexity = 1)
        {
            Id = id;
            Complexity = complexity;
            Name = name;
        }
    }
}
