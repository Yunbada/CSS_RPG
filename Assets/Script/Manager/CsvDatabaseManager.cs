using System.IO;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

[System.Serializable]
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
    public int Gold;              // 재화 (원)
    public string InventoryData;  // "itemId:count;itemId:count;..." 형식
    public string EquipmentData;  // "Weapon:1001;Helmet:0;..." 형식 (세미콜론 구분)
    public string Nickname;       // 플레이어 닉네임
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        if (Instance == null)
        {
            var go = new GameObject("CsvDatabaseManager");
            Instance = go.AddComponent<CsvDatabase>();
            DontDestroyOnLoad(go);
            Debug.Log("[CsvDatabase] 자동 생성 완료");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) 
        { 
            Destroy(gameObject); 
            return; 
        }
        else if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        filePath = Application.dataPath + "/PlayerData.csv";
        EnsureFileExists();
        LoadAllDataToCache();
    }

    private void EnsureFileExists()
    {
        if (!File.Exists(filePath))
        {
            string header = "PersonalCode,ID,Password,Level,Exp,ClassIndex,Leather,Tooth,Skull,Gold,InventoryData,EquipmentData,Nickname\n";
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
                // Gold 컬럼 (하위 호환)
                if (cols.Length > 9) int.TryParse(cols[9], out d.Gold);
                
                // InventoryData: 세미콜론 구분이므로 cols[10]에 안전하게 들어감
                d.InventoryData = cols.Length > 10 ? cols[10] : "";
                
                // EquipmentData & Nickname 스마트 파싱:
                // 기존 데이터에서 EquipmentData가 쉼표를 포함할 수 있으므로,
                // 마지막 컬럼 = Nickname, 중간(cols[11] ~ cols[끝-1]) = EquipmentData로 처리
                if (cols.Length >= 13)
                {
                    // 마지막 컬럼이 닉네임
                    d.Nickname = cols[cols.Length - 1];
                    
                    // cols[11] ~ cols[끝-2]를 합쳐서 EquipmentData 복원
                    // (기존 쉼표 구분 데이터와 새 세미콜론 구분 데이터 모두 처리)
                    var equipParts = new System.Text.StringBuilder();
                    for (int c = 11; c < cols.Length - 1; c++)
                    {
                        if (c > 11) equipParts.Append(';'); // 세미콜론으로 재결합
                        equipParts.Append(cols[c]);
                    }
                    d.EquipmentData = equipParts.ToString();
                }
                else if (cols.Length == 12)
                {
                    // cols[11]이 EquipmentData 또는 Nickname일 수 있음
                    // EquipmentData는 "Weapon:" 같은 패턴을 포함하므로 구분 가능
                    string lastCol = cols[11];
                    if (lastCol.Contains(":"))
                    {
                        d.EquipmentData = lastCol;
                        d.Nickname = "";
                    }
                    else
                    {
                        d.EquipmentData = "";
                        d.Nickname = lastCol;
                    }
                }
                else
                {
                    d.EquipmentData = cols.Length > 11 ? cols[11] : "";
                    d.Nickname = "";
                }
                
                cachedData[d.ID] = d;
                Debug.Log($"[CsvDatabase] 로드: {d.ID} (Lv{d.Level}, Class{d.ClassIndex}, Equip='{d.EquipmentData}', Nick='{d.Nickname}')");
            }
        }
    }

    public void SaveCacheToFile()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("PersonalCode,ID,Password,Level,Exp,ClassIndex,Leather,Tooth,Skull,Gold,InventoryData,EquipmentData,Nickname");
        foreach(var kvp in cachedData)
        {
            var d = kvp.Value;
            // InventoryData와 EquipmentData에 쉼표가 섞이지 않도록 세미콜론/콜론 구분자 사용 중
            string inv = d.InventoryData ?? "";
            string equip = d.EquipmentData ?? "";
            string nick = d.Nickname ?? "";
            sb.AppendLine($"{d.PersonalCode},{d.ID},{d.PW},{d.Level},{d.Exp},{d.ClassIndex},{d.Leather},{d.Tooth},{d.Skull},{d.Gold},{inv},{equip},{nick}");
        }
        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    public bool RegisterUser(string id, string pw, string nickname)
    {
        if (cachedData.ContainsKey(id)) return false;

        string newCode = "CODE-" + (cachedData.Count + 1).ToString("D4");
        UserData newUser = new UserData
        {
            PersonalCode = newCode,
            ID = id, PW = pw, Nickname = nickname,
            Level = 1, Exp = 0, ClassIndex = 0,
            Leather = 0, Tooth = 0, Skull = 0,
            Gold = 0,
            InventoryData = "", EquipmentData = ""
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
        if (target == null || string.IsNullOrEmpty(target.ID)) return;
        
        // upsert: 캐시에 없으면 신규 추가, 있으면 갱신
        cachedData[target.ID] = target;
        SaveCacheToFile();
        Debug.Log($"[CsvDatabase] SaveUser 완료: {target.ID} (Lv{target.Level}, Class{target.ClassIndex}, Gold{target.Gold})");
    }
}
