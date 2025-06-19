#if GGEMCO_USE_SPINE
using UnityEngine;

namespace GGemCo.Scripts
{
    public abstract class UISpineMetadata
    {
        public readonly UISpineManager.UISpineUid SpineUid = UISpineManager.UISpineUid.None;
        public readonly bool Loop = false;
        public Vector2 Position = Vector2.zero;
    }
    public class UISpineManager : MonoBehaviour
    {
        public enum UISpineUid 
        {
            None,
            SpineBackendLoadingIcon,
            SpineUpgradeAtkButton,
        }

        public Spine2dUIController[] uiSpines;

        private void Start()
        {
            foreach (var spine2dUIController in uiSpines)
            {
                if (spine2dUIController == null) continue;
                spine2dUIController.StopAnimation();
                spine2dUIController.gameObject.SetActive(false);
            }
        }
        private void ShowSpineByUid(bool show, UISpineMetadata uiSpineMetadata)
        {
            UISpineUid uid = uiSpineMetadata.SpineUid;
            if (uid == UISpineUid.None) return;
            Spine2dUIController spine2dUIController = uiSpines[(int)uid];
            if (spine2dUIController == null) return;
            
            if (show)
            {
                bool loop = uiSpineMetadata.Loop;
                Vector2 position = uiSpineMetadata.Position;
                spine2dUIController.gameObject.SetActive(true);
                spine2dUIController.PlayAnimation("play", loop);
                if (position != Vector2.zero)
                {
                    spine2dUIController.gameObject.transform.position = position;
                }
            }
            else
            {
                spine2dUIController.StopAnimation();
                spine2dUIController.gameObject.SetActive(false);
            }
        }
    }
}
#endif