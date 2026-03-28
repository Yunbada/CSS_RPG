using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

public class UserData
{
    public string PersonalCode;
    public string ID;
    public string PW;
    public int Level;
    public int Exp;
    public int ClassIndex;
    public int Leather;
    public int Tooth;
    public int Skull;
}

public static class LocalUserData
{
    public static UserData Current;
}

public class CsvDatabase : MonoBehaviour
{
    public static CsvDatabase Instance;

    private string filePath;
    private Dictionary<string, UserData> cachedData = new Dictionary<string, UserData>();

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); return; }

        filePath = Application.dataPath + "/PlayerData.csv";
        EnsureFileExists();
        LoadAllDataToCache();
    }

    private void EnsureFileExists()
    {
        if (!File.Exists(filePath))
        {
            string header = "PersonalCode,ID,Password,Level,Exp,ClassIndex,Leather,Tooth,Skull\n";
            File.WriteAllText(filePath, header, Encoding.UTF8);
            Debug.Log($"Created new database file at: {filePath}");
        }
    }

    private void LoadAllDataToCache()
    {
        cachedData.Clear();
        if (!File.Exists(filePath)) return;

        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            string[] cols = lines[i].Split(',');
            if (cols.Length >= 9)
            {
                UserData d = new UserData();
                d.PersonalCode = cols[0];
                d.ID = cols[1];
                d.PW = cols[2];
                int.TryParse(cols[3], out d.Level);
                int.TryParse(cols[4], out d.Exp);
                int.TryParse(cols[5], out d.ClassIndex);
                int.TryParse(cols[6], out d.Leather);
                int.TryParse(cols[7], out d.Tooth);
                int.TryParse(cols[8], out d.Skull);
                cachedData[d.ID] = d;
            }
        }
    }

    public void SaveCacheToFile()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("PersonalCode,ID,Password,Level,Exp,ClassIndex,Leather,Tooth,Skull");
        foreach(var kvp in cachedData)
        {
            var d = kvp.Value;
            sb.AppendLine($"{d.PersonalCode},{d.ID},{d.PW},{d.Level},{d.Exp},{d.ClassIndex},{d.Leather},{d.Tooth},{d.Skull}");
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public bool RegisterUser(string id, string pw)
    {
        if (cachedData.ContainsKey(id)) return false;

        string newCode = "CODE-" + (cachedData.Count + 1).ToString("D4");
        UserData newUser = new UserData
        {
            PersonalCode = newCode,
            ID = id, PW = pw, Level = 1, Exp = 0, ClassIndex = 0, Leather = 0, Tooth = 0, Skull = 0
        };
        
        cachedData[id] = newUser;
        SaveCacheToFile();
        return true;
    }

    public UserData LoginUser(string id, string pw)
    {
        if (cachedData.TryGetValue(id, out UserData user))
        {
            if (user.PW == pw) return user;
        }
        return null;
    }

    public void SaveUser(UserData target)
    {
        if (target == null) return;
        if (cachedData.ContainsKey(target.ID))
        {
            cachedData[target.ID] = target;
            SaveCacheToFile();
        }
    }
}
