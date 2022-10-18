using System.Diagnostics;

namespace KerbalHealth
{
    internal class IterationTimer
    {
        string name;
        Stopwatch timer = new Stopwatch();
        int counter;
        int reportPeriod = 1;

        public IterationTimer(string name, int reportPeriod = 1)
        {
            this.name = name;
            this.reportPeriod = reportPeriod;
        }

        public void Start()
        {
            counter++;
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
            if (counter % reportPeriod == 0)
                Report();
        }

        public void Report() =>
            Core.Log($"Timer {name}: {counter} iterations, {(float)timer.ElapsedMilliseconds / counter:F2} ms per iteration.", LogLevel.Important);
    }
}
