using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo.Scripts
{
    public interface IUidName
    {
        int Uid { get; }
        string Name { get; }
    }
    public class DefaultTable
    {
        private readonly Dictionary<int, Dictionary<string, string>> table = new Dictionary<int, Dictionary<string, string>>();
        private static readonly Dictionary<string, ConfigCommon.SuffixType> MapSuffix;
        private static readonly Dictionary<string, CurrencyConstants.Type> MapCurrencyType;
        private static readonly Dictionary<string, CharacterConstants.CharacterFacing> MapCharacterFacing;

        public virtual bool TryGetDataByUid(int uid, out object info)
        {
            info = null;
            return false;
        }
        static DefaultTable()
        {
            MapSuffix = new Dictionary<string, ConfigCommon.SuffixType>
            {
                { "PLUS", ConfigCommon.SuffixType.Plus },
                { "MINUS", ConfigCommon.SuffixType.Minus },
                { "INCREASE", ConfigCommon.SuffixType.Increase },
                { "DECREASE", ConfigCommon.SuffixType.Decrease },
            };
            MapCurrencyType = new Dictionary<string, CurrencyConstants.Type>
            {
                { "Gold", CurrencyConstants.Type.Gold },
                { "Silver", CurrencyConstants.Type.Silver },
            };
            MapCharacterFacing = new Dictionary<string, CharacterConstants.CharacterFacing>
            {
                { "Left", CharacterConstants.CharacterFacing.Left },
                { "Right", CharacterConstants.CharacterFacing.Right },
            };
        }

        protected static ConfigCommon.SuffixType ConvertSuffixType(string type) =>
            MapSuffix.GetValueOrDefault(type, ConfigCommon.SuffixType.None);

        protected static CurrencyConstants.Type ConvertCurrencyType(string type) =>
            MapCurrencyType.GetValueOrDefault(type, CurrencyConstants.Type.None);

        protected static CharacterConstants.CharacterFacing ConvertFacing(string type) =>
            MapCharacterFacing.GetValueOrDefault(type, CharacterConstants.CharacterFacing.Left);
        public virtual void LoadData(string content)
        {
            PreLoad();
            
            string[] lines = content.Split('\n');
            string[] headers = lines[0].Trim().Split('\t');

            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]) || lines[i].StartsWith("#")) continue;
                string[] values = lines[i].Split('\t');
                var data = new Dictionary<string, string>();

                for (int j = 0; j < headers.Length; j++)
                {
                    data[headers[j].Trim()] = CheckNone(values[j].Trim().Replace(@"\n", "\n"));
                }

                int uid = int.Parse(values[0]);
                table[uid] = data;

                OnLoadedData(data);
            }
        }
        
        protected virtual void PreLoad()
        {
            
        }

        /// <summary>
        /// xx,xx,xx 타입을 int 배열로 변환
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static int[] ConvertIntArray(string value)
        {
            if (value == "0") return Array.Empty<int>();
            string[] values = value.Split(',');
            int[] intArray = new int[values.Length];
            for (int i = 0; i < values.Length; i++)
            {
                intArray[i] = int.Parse(values[i]);
            }
            return intArray;
        }

        protected virtual void OnLoadedData(Dictionary<string, string> data)
        {
            
        }

        private string CheckNone(string value)
        {
            return (value == "None" || value == "NONE") ? "" : value;
        }
        public Dictionary<int, Dictionary<string, string>> GetDatas() => table;
        protected Dictionary<string, string> GetData(int uid)
        {
            Dictionary<string, string> data = table.GetValueOrDefault(uid);
            if (data == null)
            {
                GcLogger.LogError($"테이블에 정보가 없습니다. uid: {uid}");
            }
            return data;
        }
        protected string GetDataColumn(int uid, string columnName)
        {
            table.TryGetValue(uid, out var data);
            if (data == null)
            {
                return null;
            }

            data.TryGetValue(columnName, out var value);
            return value == null ? null : CheckNone(value);
        }

        protected Vector2 ConvertVector2(string value)
        {
            Vector2 position = new Vector2(0, 0);
            if (value != "")
            {
                var result2 = value.Split(",");
                position.x = float.Parse(result2[0]);
                position.y = float.Parse(result2[1]);
            }
            return position;
        }
        protected GameObject LoadPrefab(string prefabPath) {
            if (prefabPath == "") {
                GcLogger.LogError("prefab 경로가 없습니다. prefabPath: "+prefabPath+"");
                return null;
            }
            GameObject prefab = Resources.Load<GameObject>(prefabPath);
            if (prefab == null) {
                GcLogger.LogError("prefab 오브젝트가 없습니다. prefabPath: "+prefabPath);
                return null;
            }
            return prefab;
        }

        protected bool ConvertBoolean(string value)
        {
            return value == "Y";
        }

        public int GetCount()
        {
            return table.Count;
        }
    }
}