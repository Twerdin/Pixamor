using System;
using UnityEngine;

namespace Pixamor
{
    public enum CrisisType { Flood, Drought, Wildfire, Heatwave }

    [Serializable]
    public class InterventionData
    {
        public string Title;
        public int Cost;
        public int HappinessChange;
        public int ClimateChange;
        public string Result;
        public bool IsIntervention;

        public InterventionData(string title, int cost, int happiness, int climate,
            string result, bool isIntervention = true)
        {
            Title = title;
            Cost = cost;
            HappinessChange = happiness;
            ClimateChange = climate;
            Result = result;
            IsIntervention = isIntervention;
        }
    }

    [Serializable]
    public class CrisisData
    {
        public string Title;
        public string Region;
        public string Description;
        public CrisisType Type;
        // Harita eklendiğinde kullanılacak, sol üstten başlayan 0–1 konumu.
        public Vector2 MapPosition;
        public float ResponseDuration = 30f;
        public InterventionData[] Options;
        public InterventionData TimeoutResult;
    }

    // Yazılmış olay verisi ile o turdaki kalan süreyi birbirinden ayırıyoruz.
    public class ActiveCrisis
    {
        public int Id { get; }
        public CrisisData Data { get; }
        public float Deadline { get; }
        public bool IsResolved { get; internal set; }

        public ActiveCrisis(int id, CrisisData data, float spawnTime)
        {
            Id = id;
            Data = data;
            Deadline = spawnTime + data.ResponseDuration;
        }
    }
}
