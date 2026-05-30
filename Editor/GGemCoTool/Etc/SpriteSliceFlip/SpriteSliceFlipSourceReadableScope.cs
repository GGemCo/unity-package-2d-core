#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Texture2D 픽셀 접근을 위해 원본 TextureImporter의 Read/Write Enabled 값을 임시로 활성화합니다.
    /// </summary>
    internal sealed class SpriteSliceFlipSourceReadableScope : IDisposable
    {
        /// <summary>
        /// 원본 텍스처 에셋 경로입니다.
        /// </summary>
        private readonly string _assetPath;

        /// <summary>
        /// 원본 TextureImporter입니다.
        /// </summary>
        private readonly TextureImporter _importer;

        /// <summary>
        /// 작업 시작 전 Read/Write Enabled 값입니다.
        /// </summary>
        private readonly bool _originalReadable;

        /// <summary>
        /// Dispose 시 원래 Read/Write Enabled 값으로 복구할지 여부입니다.
        /// </summary>
        private readonly bool _restoreOnDispose;

        /// <summary>
        /// Read/Write Enabled 값을 변경했는지 여부입니다.
        /// </summary>
        private readonly bool _changedReadable;

        /// <summary>
        /// 원본 TextureImporter Read/Write Enabled 값을 임시 활성화합니다.
        /// </summary>
        /// <param name="sourceTexture">픽셀을 읽을 원본 텍스처입니다.</param>
        /// <param name="restoreOnDispose">Dispose 시 원래 값으로 복구할지 여부입니다.</param>
        public SpriteSliceFlipSourceReadableScope(Texture2D sourceTexture, bool restoreOnDispose)
        {
            if (sourceTexture == null)
            {
                throw new ArgumentNullException(nameof(sourceTexture));
            }

            _assetPath = AssetDatabase.GetAssetPath(sourceTexture);
            _importer = AssetImporter.GetAtPath(_assetPath) as TextureImporter;
            _restoreOnDispose = restoreOnDispose;
            if (_importer == null)
            {
                throw new InvalidOperationException("원본 TextureImporter를 찾을 수 없습니다: " + _assetPath);
            }

            _originalReadable = _importer.isReadable;
            if (!_importer.isReadable)
            {
                _importer.isReadable = true;
                _importer.SaveAndReimport();
                _changedReadable = true;
            }
        }

        /// <summary>
        /// Read/Write Enabled 값을 변경 전 상태로 복구합니다.
        /// </summary>
        public void Dispose()
        {
            if (_importer == null || !_changedReadable || !_restoreOnDispose)
            {
                return;
            }

            _importer.isReadable = _originalReadable;
            _importer.SaveAndReimport();
        }
    }
}
#endif
