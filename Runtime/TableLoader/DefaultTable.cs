using System;
using System.Collections.Generic;
using UnityEngine;

namespace GGemCo2DCore
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
        private static readonly Dictionary<string, CharacterConstants.FacingDirection8> MapCharacterFacing;
        private static readonly Dictionary<string, ConfigCommon.AnimationController> MapAnimationController;
        private static readonly Dictionary<string, ConfigCommon.PositionYType> MapPositionYType;

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
            MapCharacterFacing = new Dictionary<string, CharacterConstants.FacingDirection8>
            {
                { "Left", CharacterConstants.FacingDirection8.Left },
                { "Right", CharacterConstants.FacingDirection8.Right },
            };
            MapAnimationController = new Dictionary<string, ConfigCommon.AnimationController>
            {
                { "Sprite", ConfigCommon.AnimationController.Sprite },
                { "Spine", ConfigCommon.AnimationController.Spine },
            };
            MapPositionYType = new Dictionary<string, ConfigCommon.PositionYType>
            {
                { "CharacterHeight", ConfigCommon.PositionYType.CharacterHeight },
            };
        }
        protected static ConfigCommon.SuffixType ConvertSuffixType(string value) =>
            MapSuffix.GetValueOrDefault(value, ConfigCommon.SuffixType.None);

        protected static CurrencyConstants.Type ConvertCurrencyType(string value) =>
            MapCurrencyType.GetValueOrDefault(value, CurrencyConstants.Type.None);

        protected static CharacterConstants.FacingDirection8 ConvertFacing(string value) =>
            MapCharacterFacing.GetValueOrDefault(value, CharacterConstants.FacingDirection8.Left);
        protected static ConfigCommon.AnimationController ConvertAnimationController(string value) =>
            MapAnimationController.GetValueOrDefault(value, ConfigCommon.AnimationController.Sprite);
        protected static ConfigCommon.PositionYType ConvertPositionYType(string value) =>
            MapPositionYType.GetValueOrDefault(value, ConfigCommon.PositionYType.None);
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