using System;
using UnityEngine;

namespace GGemCo.Scripts
{
    
    [Serializable]
    public struct Vec3
    {
        public float x, y, z;

        public Vec3(Vector3 v)
        {
            x = v.x;
            y = v.y;
            z = v.z;
        }

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }
    [Serializable]
    public struct Vec2
    {
        public float x, y;

        public Vec2(Vector2 v)
        {
            x = v.x;
            y = v.y;
        }

        public Vector2 ToVector2() => new Vector2(x, y);
    }
    public static class MathHelper
    {
        public static string FormatNumber(long number)
        {
            string numberStr = number.ToString();
            int length = numberStr.Length;

            if (length > 20) // 해 단위
            {
                return InsertDecimal(numberStr, length - 20) + "해";
            }
            else if (length > 16) // 경 단위
            {
                return InsertDecimal(numberStr, length - 16) + "경";
            }
            else if (length > 12) // 조 단위
            {
                return InsertDecimal(numberStr, length - 12) + "조";
            }
            else if (length > 8) // 억 단위
            {
                return InsertDecimal(numberStr, length - 8) + "억";
            }
            else if (length > 4) // 만 단위
            {
                return InsertDecimal(numberStr, length - 4) + "만";
            }
            else
            {
                return numberStr; // 만 단위 이하의 숫자는 그대로 반환
            }
        }
        
        private static string InsertDecimal(string numberStr, int position)
        {
            // 숫자의 자릿수에 맞게 소수점을 삽입 (최대 2자리 소수)
            if (position <= 0 || position >= numberStr.Length) return numberStr;

            string integerPart = numberStr.Substring(0, position); // 정수 부분
            string fractionalPart = numberStr.Substring(position); // 소수 부분

            // 소수점 뒤에 2자리까지만 표시
            if (fractionalPart.Length > 2)
            {
                fractionalPart = fractionalPart.Substring(0, 2);
            }

            return integerPart + "." + fractionalPart;
        }
        /// <summary>
        /// y 위치 값으로 sorting order 구하기
        /// </summary>
        /// <param name="maxY">게임 월드에서 캐릭터가 있을 수 있는 y 좌표의 범위를 정의합니다.</param>
        /// <param name="positionY"></param>
        /// <returns></returns>
        public static int GetSortingOrder(float maxY, float positionY)
        {
            float minY = 0;
            
            // SpriteRenderer의 정렬 순서 범위 설정
            int minOrder = 1;
            int maxOrder = 10000;
    
            // 현재 y 좌표를 0~1 범위로 정규화합니다.
            // (maxY에서 minY로 갈수록 값이 커지도록 InverseLerp 사용)
            float normalizedY = Mathf.InverseLerp(maxY, minY, positionY);

            // 정규화된 값을 minOrder ~ maxOrder 범위에 맞게 매핑합니다.
            return minOrder + Mathf.RoundToInt(normalizedY * (maxOrder - minOrder));
        }
        /// <summary>
        /// Panel 이 화면 밖으로 벗어날 경우 보정하기
        /// </summary>
        /// <param name="rectTransform"></param>
        public static void ClampToScreen(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            Vector3 pos = rectTransform.position;

            // 좌측
            if (corners[0].x < 0)
                pos.x += -corners[0].x;

            // 우측
            if (corners[2].x > screenWidth)
                pos.x -= corners[2].x - screenWidth;

            // 아래
            if (corners[0].y < 0)
                pos.y += -corners[0].y;

            // 위
            if (corners[1].y > screenHeight)
                pos.y -= corners[1].y - screenHeight;

            rectTransform.position = pos;
        }
    }
}