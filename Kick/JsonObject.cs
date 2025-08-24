using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;


public class JsonObject
{
    public string client_id { get; set; }
    public string client_secret { get; set; }
    public string chatroomName { get; set; }
}
public class TokenResponse
{
    public string access_token { get; set; }
    public string refresh_token { get; set; }
}

public class JsonSO
{
    public DateTime lastReset { get; set; }  // store the last 00/30 reset time
    public List<string> alreadyCheck { get; set; }
    public List<History_SO> history { get; set; }
    public static string fileName = "JsonSO.json";

    public class History_SO
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    private DateTime GetCurrentResetSlot()
    {
        var now = DateTime.Now;
        int minuteSlot = (now.Minute / 15) * 15;
        return new DateTime(now.Year, now.Month, now.Day, now.Hour, minuteSlot, 0);
    }

    public void Reset()
    {
        lastReset = GetCurrentResetSlot(); // align to slot
        alreadyCheck.Clear();
    }

    public JsonSO LoadFile()
    {
        if (File.Exists(fileName))
        {
            alreadyCheck = new();
            history = new();
            var so = JsonConvert.DeserializeObject<JsonSO>(File.ReadAllText(fileName));

            // If we've passed into a new 00/30 slot
            if (GetCurrentResetSlot() != so.lastReset)
            {
                so.Reset();
            }
            return so;
        }
        else
        {
            alreadyCheck = new();
            history = new();
            lastReset = GetCurrentResetSlot();
        }
        return this;
    }

    public void Check()
    {
        if (GetCurrentResetSlot() != lastReset)
        {
            Reset();
        }
    }

    public async Task SaveToFile()
    {
        if (GetCurrentResetSlot() != lastReset)
        {
            Reset();
        }
        await File.WriteAllTextAsync(fileName, JsonConvert.SerializeObject(this));
    }
}

public class PeopleData
{
    public List<PeopleUserData> peopleData { get; set; }
    public class PeopleUserData
    {
        public string userName { get; set; }
        public string email { get; set; }
    }
    public static string fileName = "peopleData.json";
    public PeopleData LoadFile()
    {
        if (File.Exists(fileName))
        {
            var so = JsonConvert.DeserializeObject<PeopleData>(File.ReadAllText(fileName));
            return so;
        }
        else
        {
            peopleData = new();
        }
        return this;
    }
    public async Task SaveToFile()
    {
        await File.WriteAllTextAsync(fileName, JsonConvert.SerializeObject(this));
    }
}

public class JsonBanned
{
    public List<string> userContain { get; set; }
    public List<string> msgContain { get; set; }
    public static string fileName = "banContaining.json";
    public JsonBanned LoadFile()
    {
        if (File.Exists(fileName))
        {
            var so = JsonConvert.DeserializeObject<JsonBanned>(File.ReadAllText(fileName));
            return so;
        }
        else
        {
            msgContain = new();
            userContain = new();
        }
        return this;
    }
    public async Task SaveToFile()
    {
        await File.WriteAllTextAsync(fileName, JsonConvert.SerializeObject(this));
    }
}

