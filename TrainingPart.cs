namespace KerbalHealth
{
    public class TrainingPart : IConfigNode
    {
        public const string ConfigNodeName = "TRAINING_PART";

        //public uint Id { get; set; }

        public string Name { get; set; }
        
        public double Complexity { get; set; }

        public double Level { get; set; }

        public bool OnCurrentVessel => Complexity > 0;

        public bool TrainingComplete => Level >= Core.TrainingCap;

        public bool TrainingNow => Complexity > 0 && Level < Core.TrainingCap;

        public TrainingPart(ConfigNode n) => Load(n);

        public TrainingPart(string name, double complexity = 1, double level = 0)
        {
            //Id = id;
            Complexity = complexity;
            Name = name;
            Level = level;
        }

        public void StartTraining(double complexity) => Complexity = complexity;

        public void Save(ConfigNode node)
        {
            //node.AddValue("id", Id);
            Core.Log($"Saving training part {Name}, complexity = {Complexity}, level = {Level:P2}.");
            node.AddValue("name", Name);
            if (Complexity > 0)
                node.AddValue("complexity", Complexity);
            node.AddValue("level", Level);
        }

        public void Load(ConfigNode node)
        {
            //Id = node.GetUInt("id");
            //if (Id == 0)
            //{
            //    Core.Log("Incorrect part id 0 for training part.", LogLevel.Error);
            //    return;
            //}
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
