using System;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCoreEditor
{
    /// <summary>
    /// Sprite Composer 렌더링 결과를 보관합니다.
    /// </summary>
    internal sealed class SpriteComposerRenderResult : IDisposable
    {
        /// <summary>
        /// RenderTexture에서 읽어온 합성 결과 텍스처입니다.
        /// </summary>
        public readonly Texture2D Texture;

        /// <summary>
        /// 생성된 텍스처의 가로 픽셀 수입니다.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// 생성된 텍스처의 세로 픽셀 수입니다.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// MaxTextureSize 제한을 반영한 실제 Pixels Per Unit 값입니다.
        /// </summary>
        public readonly float EffectivePixelsPerUnit;

        /// <summary>
        /// 원본 렌더링 대상 목록을 기준으로 계산한 월드 Bounds입니다.
        /// </summary>
        public readonly Bounds SourceBounds;

        /// <summary>
        /// 렌더링 결과 정보를 생성합니다.
        /// </summary>
        /// <param name="texture">합성 결과 텍스처입니다.</param>
        /// <param name="width">가로 픽셀 수입니다.</param>
        /// <param name="height">세로 픽셀 수입니다.</param>
        /// <param name="effectivePixelsPerUnit">실제 적용된 Pixels Per Unit 값입니다.</param>
        /// <param name="sourceBounds">원본 월드 Bounds입니다.</param>
        public SpriteComposerRenderResult(Texture2D texture, int width, int height, float effectivePixelsPerUnit, Bounds sourceBounds)
        {
            Texture = texture;
            Width = width;
            Height = height;
            EffectivePixelsPerUnit = effectivePixelsPerUnit;
            SourceBounds = sourceBounds;
        }

        /// <summary>
        /// 미리보기 텍스처를 즉시 파괴합니다.
        /// </summary>
        public void Dispose()
        {
            if (Texture != null)
            {
                UnityEngine.Object.DestroyImmediate(Texture);
            }
        }
    }

    /// <summary>
    /// 선택된 SpriteRenderer와 UGUI Image를 임시 카메라로 렌더링하여 하나의 Texture2D로 합성합니다.
    /// </summary>
    internal static class SpriteComposerRenderService
    {
        /// <summary>
        /// 선택된 SpriteRenderer와 UGUI Image를 투명 배경 텍스처로 렌더링합니다.
        /// </summary>
        /// <param name="selection">합성할 Hierarchy 선택 정보입니다.</param>
        /// <param name="settings">렌더링 설정입니다.</param>
        /// <returns>합성된 텍스처와 출력 정보를 담은 결과입니다.</returns>
        public static SpriteComposerRenderResult Render(SpriteComposerSelection selection, SpriteComposerSettings settings)
        {
            if (selection == null || !selection.HasRenderableItems)
            {
                throw new InvalidOperationException("합성 가능한 SpriteRenderer 또는 UI Image가 없습니다.");
            }

            settings.Normalize();

            Bounds sourceBounds;
            if (!SpriteComposerBoundsUtility.TryCalculateWorldBounds(selection.Renderers, selection.Images, out sourceBounds))
            {
                throw new InvalidOperationException("선택한 오브젝트의 Bounds를 계산할 수 없습니다.");
            }

            var renderPlan = CreateRenderPlan(sourceBounds, settings);
            var captureLayer = FindCaptureLayer();
            GameObject tempRoot = null;
            GameObject cameraObject = null;
            RenderTexture renderTexture = null;
            var previousActiveRenderTexture = RenderTexture.active;

            try
            {
                cameraObject = CreateCameraObject(renderPlan, captureLayer);
                var camera = cameraObject.GetComponent<Camera>();
                tempRoot = CreateTemporaryCopies(selection, sourceBounds.center, captureLayer, settings.IncludeInactive, camera, renderPlan);

                renderTexture = new RenderTexture(renderPlan.Width, renderPlan.Height, 24, RenderTextureFormat.ARGB32);
                renderTexture.name = "SpriteComposer_RenderTexture";
                renderTexture.hideFlags = HideFlags.HideAndDontSave;
                renderTexture.antiAliasing = settings.AntiAliasing;
                renderTexture.filterMode = settings.FilterMode;
                renderTexture.Create();

                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                GL.Clear(true, true, Color.clear);
                Canvas.ForceUpdateCanvases();
                camera.Render();

                var texture = new Texture2D(renderPlan.Width, renderPlan.Height, TextureFormat.RGBA32, false);
                texture.name = settings.GetSafeFileNameWithoutExtension();
                texture.filterMode = settings.FilterMode;
                texture.ReadPixels(new Rect(0, 0, renderPlan.Width, renderPlan.Height), 0, 0);
                texture.Apply(false, false);

                return new SpriteComposerRenderResult(texture, renderPlan.Width, renderPlan.Height, renderPlan.EffectivePixelsPerUnit, sourceBounds);
            }
            finally
            {
                RenderTexture.active = previousActiveRenderTexture;

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (tempRoot != null)
                {
                    UnityEngine.Object.DestroyImmediate(tempRoot);
                }
            }
        }

        /// <summary>
        /// Bounds와 출력 설정을 기준으로 실제 렌더링 크기와 PPU를 계산합니다.
        /// </summary>
        /// <param name="sourceBounds">선택된 렌더링 대상들의 월드 Bounds입니다.</param>
        /// <param name="settings">렌더링 설정입니다.</param>
        /// <returns>렌더링에 사용할 계산 결과입니다.</returns>
        private static RenderPlan CreateRenderPlan(Bounds sourceBounds, SpriteComposerSettings settings)
        {
            var requestedPpu = Mathf.Max(1f, settings.PixelsPerUnit);
            var paddingWorld = settings.Padding / requestedPpu;
            var worldWidth = Mathf.Max(1f / requestedPpu, sourceBounds.size.x + paddingWorld * 2f);
            var worldHeight = Mathf.Max(1f / requestedPpu, sourceBounds.size.y + paddingWorld * 2f);
            var effectivePpu = requestedPpu;

            var width = Mathf.CeilToInt(worldWidth * effectivePpu);
            var height = Mathf.CeilToInt(worldHeight * effectivePpu);
            var maxSide = Mathf.Max(width, height);
            if (maxSide > settings.MaxTextureSize)
            {
                var scale = settings.MaxTextureSize / (float)maxSide;
                effectivePpu = Mathf.Max(1f, requestedPpu * scale);
                width = Mathf.CeilToInt(worldWidth * effectivePpu);
                height = Mathf.CeilToInt(worldHeight * effectivePpu);
            }

            width = Mathf.Clamp(width, 1, settings.MaxTextureSize);
            height = Mathf.Clamp(height, 1, settings.MaxTextureSize);

            return new RenderPlan(width, height, worldWidth, worldHeight, effectivePpu);
        }

        /// <summary>
        /// 원본 오브젝트를 수정하지 않기 위해 SpriteRenderer와 UGUI Image만 가진 임시 복제본을 생성합니다.
        /// </summary>
        /// <param name="selection">복제할 선택 정보입니다.</param>
        /// <param name="sourceCenter">원본 Bounds 중심입니다.</param>
        /// <param name="captureLayer">캡처 전용 레이어 번호입니다.</param>
        /// <param name="forceVisible">비활성 대상도 강제로 렌더링할지 여부입니다.</param>
        /// <param name="camera">UI Canvas를 렌더링할 카메라입니다.</param>
        /// <param name="renderPlan">렌더링 크기와 월드 크기 계산 결과입니다.</param>
        /// <returns>임시 복제본들의 루트 오브젝트입니다.</returns>
        private static GameObject CreateTemporaryCopies(SpriteComposerSelection selection, Vector3 sourceCenter, int captureLayer, bool forceVisible, Camera camera, RenderPlan renderPlan)
        {
            var tempRoot = new GameObject("SpriteComposer_TemporaryRoot");
            tempRoot.hideFlags = HideFlags.HideAndDontSave;
            tempRoot.layer = captureLayer;
            tempRoot.transform.position = Vector3.zero;
            tempRoot.transform.rotation = Quaternion.identity;
            tempRoot.transform.localScale = Vector3.one;

            CopySpriteRendererTrees(selection, tempRoot.transform, sourceCenter, captureLayer, forceVisible);
            CopyUiImages(selection, tempRoot.transform, sourceCenter, captureLayer, forceVisible, camera, renderPlan);
            return tempRoot;
        }

        /// <summary>
        /// 선택 루트 계층을 순회하면서 SpriteRenderer에 필요한 렌더링 속성만 복제합니다.
        /// </summary>
        /// <param name="selection">복제할 선택 정보입니다.</param>
        /// <param name="parent">복제본을 붙일 부모 Transform입니다.</param>
        /// <param name="sourceCenter">원본 Bounds 중심입니다.</param>
        /// <param name="captureLayer">캡처 전용 레이어 번호입니다.</param>
        /// <param name="forceVisible">비활성 대상도 강제로 렌더링할지 여부입니다.</param>
        private static void CopySpriteRendererTrees(SpriteComposerSelection selection, Transform parent, Vector3 sourceCenter, int captureLayer, bool forceVisible)
        {
            if (selection.Roots == null || selection.Roots.Length == 0 || !selection.HasRenderableSprites)
            {
                return;
            }

            foreach (var root in selection.Roots)
            {
                if (root == null)
                {
                    continue;
                }

                CopyTransformTree(root, parent, sourceCenter, captureLayer, forceVisible, true);
            }
        }

        /// <summary>
        /// Transform 계층을 복사하면서 SpriteRenderer에 필요한 렌더링 속성만 복제합니다.
        /// </summary>
        /// <param name="source">복사할 원본 Transform입니다.</param>
        /// <param name="parent">복제본을 붙일 부모 Transform입니다.</param>
        /// <param name="sourceCenter">원본 Bounds 중심입니다.</param>
        /// <param name="captureLayer">캡처 전용 레이어 번호입니다.</param>
        /// <param name="forceVisible">비활성 대상도 강제로 렌더링할지 여부입니다.</param>
        /// <param name="isRoot">선택 루트 여부입니다.</param>
        private static void CopyTransformTree(Transform source, Transform parent, Vector3 sourceCenter, int captureLayer, bool forceVisible, bool isRoot)
        {
            var copy = new GameObject(source.gameObject.name);
            copy.hideFlags = HideFlags.HideAndDontSave;
            copy.layer = captureLayer;
            copy.transform.SetParent(parent, false);

            if (isRoot)
            {
                copy.transform.position = source.position - sourceCenter;
                copy.transform.rotation = source.rotation;
                copy.transform.localScale = source.lossyScale;
            }
            else
            {
                copy.transform.localPosition = source.localPosition;
                copy.transform.localRotation = source.localRotation;
                copy.transform.localScale = source.localScale;
            }

            CopySpriteRenderer(source, copy, forceVisible);

            for (var i = 0; i < source.childCount; i++)
            {
                CopyTransformTree(source.GetChild(i), copy.transform, sourceCenter, captureLayer, forceVisible, false);
            }
        }

        /// <summary>
        /// 원본 SpriteRenderer의 주요 렌더링 속성을 임시 복제본에 복사합니다.
        /// </summary>
        /// <param name="sourceTransform">원본 Transform입니다.</param>
        /// <param name="copyObject">SpriteRenderer를 추가할 복제 GameObject입니다.</param>
        /// <param name="forceVisible">비활성 렌더러도 강제로 표시할지 여부입니다.</param>
        private static void CopySpriteRenderer(Transform sourceTransform, GameObject copyObject, bool forceVisible)
        {
            var sourceRenderer = sourceTransform.GetComponent<SpriteRenderer>();
            if (sourceRenderer == null || sourceRenderer.sprite == null)
            {
                return;
            }

            if (!forceVisible && (!sourceRenderer.enabled || !sourceRenderer.gameObject.activeInHierarchy))
            {
                return;
            }

            var copyRenderer = copyObject.AddComponent<SpriteRenderer>();
            copyRenderer.sprite = sourceRenderer.sprite;
            copyRenderer.color = sourceRenderer.color;
            copyRenderer.flipX = sourceRenderer.flipX;
            copyRenderer.flipY = sourceRenderer.flipY;
            copyRenderer.drawMode = sourceRenderer.drawMode;
            copyRenderer.size = sourceRenderer.size;
            copyRenderer.tileMode = sourceRenderer.tileMode;
            copyRenderer.maskInteraction = sourceRenderer.maskInteraction;
            copyRenderer.spriteSortPoint = sourceRenderer.spriteSortPoint;
            copyRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
            copyRenderer.sortingOrder = sourceRenderer.sortingOrder;
            copyRenderer.sharedMaterial = sourceRenderer.sharedMaterial;
            copyRenderer.enabled = true;
        }

        /// <summary>
        /// 선택된 UGUI Image들을 임시 World Space Canvas에 복제합니다.
        /// </summary>
        /// <param name="selection">복제할 선택 정보입니다.</param>
        /// <param name="parent">Canvas를 붙일 부모 Transform입니다.</param>
        /// <param name="sourceCenter">원본 Bounds 중심입니다.</param>
        /// <param name="captureLayer">캡처 전용 레이어 번호입니다.</param>
        /// <param name="forceVisible">비활성 Image도 강제로 렌더링할지 여부입니다.</param>
        /// <param name="camera">World Space Canvas에 연결할 카메라입니다.</param>
        /// <param name="renderPlan">렌더링 크기와 월드 크기 계산 결과입니다.</param>
        private static void CopyUiImages(SpriteComposerSelection selection, Transform parent, Vector3 sourceCenter, int captureLayer, bool forceVisible, Camera camera, RenderPlan renderPlan)
        {
            if (selection.Images == null || selection.Images.Length == 0)
            {
                return;
            }

            var canvasObject = CreateUiCanvas(parent, captureLayer, camera, renderPlan);
            for (var i = 0; i < selection.Images.Length; i++)
            {
                var sourceImage = selection.Images[i];
                if (!CanCopyImage(sourceImage, forceVisible))
                {
                    continue;
                }

                CopyImage(sourceImage, canvasObject.transform, sourceCenter, captureLayer);
            }
        }

        /// <summary>
        /// UGUI Image 복제본들을 담을 임시 World Space Canvas를 생성합니다.
        /// </summary>
        /// <param name="parent">Canvas를 붙일 부모 Transform입니다.</param>
        /// <param name="captureLayer">캡처 전용 레이어 번호입니다.</param>
        /// <param name="camera">Canvas 렌더링에 사용할 카메라입니다.</param>
        /// <param name="renderPlan">렌더링 크기와 월드 크기 계산 결과입니다.</param>
        /// <returns>Canvas가 포함된 임시 GameObject입니다.</returns>
        private static GameObject CreateUiCanvas(Transform parent, int captureLayer, Camera camera, RenderPlan renderPlan)
        {
            var canvasObject = new GameObject("SpriteComposer_TemporaryUICanvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.hideFlags = HideFlags.HideAndDontSave;
            canvasObject.layer = captureLayer;
            canvasObject.transform.SetParent(parent, false);

            var canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.position = Vector3.zero;
            canvasRect.rotation = Quaternion.identity;
            canvasRect.localScale = Vector3.one;
            canvasRect.sizeDelta = new Vector2(renderPlan.WorldWidth, renderPlan.WorldHeight);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = camera;
            canvas.overrideSorting = true;
            canvas.sortingOrder = short.MaxValue;
            canvas.pixelPerfect = false;

            return canvasObject;
        }

        /// <summary>
        /// UGUI Image를 현재 설정에서 복제할 수 있는지 검사합니다.
        /// </summary>
        /// <param name="sourceImage">복제할 원본 Image입니다.</param>
        /// <param name="forceVisible">비활성 Image도 강제로 복제할지 여부입니다.</param>
        /// <returns>복제 가능하면 true입니다.</returns>
        private static bool CanCopyImage(Image sourceImage, bool forceVisible)
        {
            if (sourceImage == null || sourceImage.sprite == null)
            {
                return false;
            }

            if (forceVisible)
            {
                return true;
            }

            return sourceImage.enabled && sourceImage.gameObject.activeInHierarchy;
        }

        /// <summary>
        /// 원본 UGUI Image의 RectTransform과 표시 속성을 임시 Canvas 하위에 복사합니다.
        /// </summary>
        /// <param name="sourceImage">복제할 원본 Image입니다.</param>
        /// <param name="parent">복제본을 붙일 Canvas Transform입니다.</param>
        /// <param name="sourceCenter">원본 Bounds 중심입니다.</param>
        /// <param name="captureLayer">캡처 전용 레이어 번호입니다.</param>
        private static void CopyImage(Image sourceImage, Transform parent, Vector3 sourceCenter, int captureLayer)
        {
            var sourceRect = sourceImage.rectTransform;
            var imageObject = new GameObject(sourceImage.gameObject.name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.hideFlags = HideFlags.HideAndDontSave;
            imageObject.layer = captureLayer;
            imageObject.transform.SetParent(parent, false);

            var copyRect = imageObject.GetComponent<RectTransform>();
            copyRect.anchorMin = new Vector2(0.5f, 0.5f);
            copyRect.anchorMax = new Vector2(0.5f, 0.5f);
            copyRect.pivot = sourceRect.pivot;
            copyRect.sizeDelta = sourceRect.rect.size;
            copyRect.position = sourceRect.position - sourceCenter;
            copyRect.rotation = sourceRect.rotation;
            copyRect.localScale = sourceRect.lossyScale;

            var copyImage = imageObject.GetComponent<Image>();
            copyImage.sprite = sourceImage.sprite;
            copyImage.overrideSprite = sourceImage.overrideSprite;
            copyImage.type = sourceImage.type;
            copyImage.color = sourceImage.color;
            copyImage.material = sourceImage.material;
            copyImage.raycastTarget = false;
            copyImage.preserveAspect = sourceImage.preserveAspect;
            copyImage.fillCenter = sourceImage.fillCenter;
            copyImage.fillMethod = sourceImage.fillMethod;
            copyImage.fillOrigin = sourceImage.fillOrigin;
            copyImage.fillClockwise = sourceImage.fillClockwise;
            copyImage.fillAmount = sourceImage.fillAmount;
            copyImage.pixelsPerUnitMultiplier = sourceImage.pixelsPerUnitMultiplier;
        }

        /// <summary>
        /// 임시 복제본만 렌더링할 카메라 오브젝트를 생성합니다.
        /// </summary>
        /// <param name="renderPlan">렌더링 크기와 월드 크기 계산 결과입니다.</param>
        /// <param name="captureLayer">카메라가 렌더링할 레이어 번호입니다.</param>
        /// <returns>Camera 컴포넌트를 가진 임시 GameObject입니다.</returns>
        private static GameObject CreateCameraObject(RenderPlan renderPlan, int captureLayer)
        {
            var cameraObject = new GameObject("SpriteComposer_Camera");
            cameraObject.hideFlags = HideFlags.HideAndDontSave;
            cameraObject.layer = captureLayer;
            cameraObject.transform.position = new Vector3(0f, 0f, -1000f);
            cameraObject.transform.rotation = Quaternion.identity;

            var camera = cameraObject.AddComponent<Camera>();
            camera.enabled = false;
            camera.orthographic = true;
            camera.orthographicSize = renderPlan.WorldHeight * 0.5f;
            camera.aspect = renderPlan.Width / (float)renderPlan.Height;
            camera.nearClipPlane = 0.01f;
            camera.farClipPlane = 2000f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << captureLayer;
            camera.allowHDR = false;
            camera.allowMSAA = true;
            return cameraObject;
        }

        /// <summary>
        /// 프로젝트에서 가급적 충돌이 적은 캡처 전용 레이어를 찾습니다.
        /// </summary>
        /// <returns>캡처에 사용할 레이어 번호입니다.</returns>
        private static int FindCaptureLayer()
        {
            for (var layer = 31; layer >= 8; layer--)
            {
                if (string.IsNullOrEmpty(LayerMask.LayerToName(layer)))
                {
                    return layer;
                }
            }

            return 31;
        }

        /// <summary>
        /// 카메라 렌더링에 필요한 계산 값을 묶은 내부 구조체입니다.
        /// </summary>
        private readonly struct RenderPlan
        {
            /// <summary>
            /// 출력 가로 픽셀 수입니다.
            /// </summary>
            public readonly int Width;

            /// <summary>
            /// 출력 세로 픽셀 수입니다.
            /// </summary>
            public readonly int Height;

            /// <summary>
            /// 출력에 대응되는 월드 가로 크기입니다.
            /// </summary>
            public readonly float WorldWidth;

            /// <summary>
            /// 출력에 대응되는 월드 세로 크기입니다.
            /// </summary>
            public readonly float WorldHeight;

            /// <summary>
            /// 실제 적용된 Pixels Per Unit입니다.
            /// </summary>
            public readonly float EffectivePixelsPerUnit;

            /// <summary>
            /// 렌더링 계산 값을 생성합니다.
            /// </summary>
            /// <param name="width">출력 가로 픽셀 수입니다.</param>
            /// <param name="height">출력 세로 픽셀 수입니다.</param>
            /// <param name="worldWidth">출력 월드 가로 크기입니다.</param>
            /// <param name="worldHeight">출력 월드 세로 크기입니다.</param>
            /// <param name="effectivePixelsPerUnit">실제 적용된 PPU입니다.</param>
            public RenderPlan(int width, int height, float worldWidth, float worldHeight, float effectivePixelsPerUnit)
            {
                Width = width;
                Height = height;
                WorldWidth = worldWidth;
                WorldHeight = worldHeight;
                EffectivePixelsPerUnit = effectivePixelsPerUnit;
            }
        }
    }
}
