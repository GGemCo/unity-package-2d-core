using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GGemCo2DCore
{
    /// <summary>
    /// UIIcon의 hover 이미지와 선택 이미지 표시를 담당합니다.
    /// </summary>
    internal sealed class UIWindowIconVisualPresenter
    {
        private readonly Dictionary<GameObject, SelectedIconImageInstance> _selectedIconImageByPrefab =
            new Dictionary<GameObject, SelectedIconImageInstance>();
        private GameObject _prefabIconOver;
        private GameObject _prefabIconSelected;
        private Image _imageIconOver;
        private Image _imageIconSelected;
        private SelectedIconImageInstance _activeSelectedIconImage;
        private bool _isSelectedIconSizeFixed;
        private UISelectedIconAnimationSettings _defaultSelectedIconAnimation;

        /// <summary>
        /// 선택 이미지 프리팹으로 생성한 인스턴스와 기본 Sprite를 함께 보관합니다.
        /// </summary>
        private sealed class SelectedIconImageInstance
        {
            public Image image;
            public Sprite defaultSprite;
        }

        /// <summary>
        /// Presenter가 사용할 프리팹과 기본 표시 설정을 갱신합니다.
        /// </summary>
        /// <param name="prefabIconOver">마우스 오버 시 표시할 프리팹입니다.</param>
        /// <param name="prefabIconSelected">선택 시 표시할 기본 프리팹입니다.</param>
        /// <param name="isSelectedIconSizeFixed">선택 이미지 크기를 슬롯 크기에 맞추지 않을지 여부입니다.</param>
        /// <param name="defaultSelectedIconAnimation">기본 선택 이미지 애니메이션 설정입니다.</param>
        public void Configure(
            GameObject prefabIconOver,
            GameObject prefabIconSelected,
            bool isSelectedIconSizeFixed,
            UISelectedIconAnimationSettings defaultSelectedIconAnimation)
        {
            _prefabIconOver = prefabIconOver;
            _prefabIconSelected = prefabIconSelected;
            _isSelectedIconSizeFixed = isSelectedIconSizeFixed;
            _defaultSelectedIconAnimation = defaultSelectedIconAnimation;
        }

        /// <summary>
        /// hover 이미지 프리팹을 캔버스 아래에 생성하고 숨김 상태로 초기화합니다.
        /// </summary>
        public void MakeIconOver()
        {
            if (_prefabIconOver == null || _imageIconOver != null)
            {
                return;
            }

            Transform parent = ResolveSelectedIconParent(null);
            if (parent == null)
            {
                return;
            }

            _imageIconOver = UnityEngine.Object.Instantiate(_prefabIconOver, parent)?.GetComponent<Image>();
            if (_imageIconOver == null)
            {
                return;
            }

            _imageIconOver.gameObject.SetActive(false);
        }

        /// <summary>
        /// 기본 선택 이미지 프리팹을 캔버스 아래에 생성하고 숨김 상태로 초기화합니다.
        /// </summary>
        public void MakeIconSelected()
        {
            if (_prefabIconSelected == null)
            {
                return;
            }

            _activeSelectedIconImage = GetOrCreateSelectedIconImage(_prefabIconSelected);
            _imageIconSelected = _activeSelectedIconImage?.image;
        }

        /// <summary>
        /// hover 이미지의 표시 상태, 위치, 크기를 갱신합니다.
        /// </summary>
        /// <param name="show">hover 이미지를 표시하면 true입니다.</param>
        /// <param name="position">표시할 월드 좌표입니다. null이면 기존 위치를 유지합니다.</param>
        /// <param name="slotSize">표시할 크기입니다. null이면 기존 크기를 유지합니다.</param>
        public void ShowOverIconImage(bool show, Vector2? position = null, Vector2? slotSize = null)
        {
            if (_imageIconOver == null)
            {
                return;
            }

            _imageIconOver.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            if (position.HasValue)
            {
                _imageIconOver.rectTransform.position = position.Value;
            }

            if (slotSize.HasValue)
            {
                _imageIconOver.rectTransform.sizeDelta = slotSize.Value;
            }
        }

        /// <summary>
        /// 선택 이미지의 표시 상태와 위치, 크기, Sprite, 애니메이션을 갱신합니다.
        /// </summary>
        /// <param name="show">선택 이미지를 표시하면 true입니다.</param>
        /// <param name="position">선택 이미지가 표시될 월드 좌표입니다. null이면 기존 위치를 유지합니다.</param>
        /// <param name="slotSize">선택 이미지 크기입니다. null이면 기존 크기를 유지합니다.</param>
        /// <param name="spriteOverride">선택 이미지에 사용할 Sprite입니다. null이면 프리팹 기본 Sprite를 사용합니다.</param>
        /// <param name="prefabOverride">선택 이미지에 사용할 Prefab입니다. null이면 기본 Prefab을 사용합니다.</param>
        /// <param name="animationOverride">선택 이미지 애니메이션 설정입니다. null이면 기본 설정을 사용합니다.</param>
        /// <param name="parentOverride">선택 이미지 오브젝트를 붙일 부모 Transform입니다. null이면 메인 캔버스를 사용합니다.</param>
        public void ShowSelectIconImage(
            bool show,
            Vector2? position = null,
            Vector2? slotSize = null,
            Sprite spriteOverride = null,
            GameObject prefabOverride = null,
            UISelectedIconAnimationSettings animationOverride = null,
            Transform parentOverride = null)
        {
            GameObject prefab = prefabOverride != null ? prefabOverride : _prefabIconSelected;
            SelectedIconImageInstance selectedIconImage = GetOrCreateSelectedIconImage(prefab);
            if (selectedIconImage == null || selectedIconImage.image == null)
            {
                return;
            }

            if (_activeSelectedIconImage != null &&
                _activeSelectedIconImage != selectedIconImage &&
                _activeSelectedIconImage.image != null)
            {
                ApplySelectedIconParent(_activeSelectedIconImage, null);
                _activeSelectedIconImage.image.gameObject.SetActive(false);
            }

            _activeSelectedIconImage = selectedIconImage;
            _imageIconSelected = selectedIconImage.image;

            ApplySelectedIconParent(selectedIconImage, show ? parentOverride : null);

            _imageIconSelected.gameObject.SetActive(show);
            if (!show)
            {
                return;
            }

            ApplySelectedIconSprite(selectedIconImage, spriteOverride);

            VfxEffectUI vfxEffect = _imageIconSelected.GetComponent<VfxEffectUI>();
            Animation2dController animation2dController = _imageIconSelected.GetComponent<Animation2dController>();
            if (vfxEffect != null)
            {
                vfxEffect.PlayEffect(true);
            }

            PlaySelectedIconAnimation(animation2dController, animationOverride);

            if (parentOverride != null)
            {
                _imageIconSelected.rectTransform.anchoredPosition = Vector2.zero;
            }
            else if (position.HasValue)
            {
                _imageIconSelected.rectTransform.position = position.Value;
            }

            if (slotSize.HasValue && !_isSelectedIconSizeFixed)
            {
                _imageIconSelected.rectTransform.sizeDelta = slotSize.Value;
            }
        }

        /// <summary>
        /// 선택 이미지 오브젝트를 요청된 부모 아래로 이동합니다.
        /// </summary>
        /// <param name="instance">부모를 변경할 선택 이미지 인스턴스 정보입니다.</param>
        /// <param name="parentOverride">선택 이미지가 붙을 부모 Transform입니다. null이면 메인 캔버스를 사용합니다.</param>
        private void ApplySelectedIconParent(SelectedIconImageInstance instance, Transform parentOverride)
        {
            if (instance == null || instance.image == null)
            {
                return;
            }

            Transform parent = ResolveSelectedIconParent(parentOverride);
            if (parent == null || instance.image.transform.parent == parent)
            {
                return;
            }

            instance.image.transform.SetParent(parent, false);
        }

        /// <summary>
        /// 선택 이미지가 배치될 최종 부모 Transform을 반환합니다.
        /// </summary>
        /// <param name="parentOverride">윈도우 또는 아이콘에서 요청한 부모 Transform입니다.</param>
        /// <returns>선택 이미지가 배치될 부모 Transform입니다.</returns>
        private Transform ResolveSelectedIconParent(Transform parentOverride)
        {
            if (parentOverride != null)
            {
                return parentOverride;
            }

            return SceneGame.Instance != null && SceneGame.Instance.canvasUI != null
                ? SceneGame.Instance.canvasUI.transform
                : null;
        }

        /// <summary>
        /// 선택 이미지의 2D 애니메이션을 요청된 설정으로 재생합니다.
        /// </summary>
        /// <param name="animation2dController">선택 이미지에 연결된 2D 애니메이션 컨트롤러입니다.</param>
        /// <param name="animationOverride">윈도우 또는 아이콘 상태에서 전달한 애니메이션 오버라이드 설정입니다.</param>
        private void PlaySelectedIconAnimation(
            Animation2dController animation2dController,
            UISelectedIconAnimationSettings animationOverride)
        {
            if (animation2dController == null)
            {
                return;
            }

            UISelectedIconAnimationSettings animationSettings =
                animationOverride ?? _defaultSelectedIconAnimation;
            if (animationSettings == null || !animationSettings.HasAnimation)
            {
                return;
            }

            animation2dController.PlayAnimation(animationSettings.animationName, animationSettings.isLoop);
        }

        /// <summary>
        /// 선택 이미지 Prefab 인스턴스를 조회하거나 생성합니다.
        /// </summary>
        /// <param name="prefab">선택 이미지로 사용할 Prefab입니다.</param>
        /// <returns>선택 이미지 인스턴스 정보입니다. 생성할 수 없으면 null을 반환합니다.</returns>
        private SelectedIconImageInstance GetOrCreateSelectedIconImage(GameObject prefab)
        {
            if (prefab == null)
            {
                return null;
            }

            if (_selectedIconImageByPrefab.TryGetValue(prefab, out SelectedIconImageInstance cached))
            {
                if (cached != null && cached.image != null)
                {
                    return cached;
                }

                _selectedIconImageByPrefab.Remove(prefab);
            }

            Transform parent = ResolveSelectedIconParent(null);
            if (parent == null)
            {
                return null;
            }

            Image image = UnityEngine.Object.Instantiate(prefab, parent)?.GetComponent<Image>();
            if (image == null)
            {
                return null;
            }

            image.gameObject.SetActive(false);
            SelectedIconImageInstance instance = new SelectedIconImageInstance
            {
                image = image,
                defaultSprite = image.sprite,
            };
            _selectedIconImageByPrefab[prefab] = instance;
            return instance;
        }

        /// <summary>
        /// 선택 이미지 Sprite override를 적용하거나 프리팹 기본 Sprite로 되돌립니다.
        /// </summary>
        /// <param name="instance">선택 이미지 인스턴스 정보입니다.</param>
        /// <param name="spriteOverride">적용할 Sprite입니다. null이면 프리팹 기본 Sprite를 적용합니다.</param>
        private static void ApplySelectedIconSprite(SelectedIconImageInstance instance, Sprite spriteOverride)
        {
            if (instance == null || instance.image == null)
            {
                return;
            }

            instance.image.sprite = spriteOverride != null ? spriteOverride : instance.defaultSprite;
        }
    }
}
