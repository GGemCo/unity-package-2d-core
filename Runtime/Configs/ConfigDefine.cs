using System.Collections.Generic;

namespace GGemCo2DCore
{
    public static class ConfigDefine
    {
        public const string NameSDK = "GGemCo";
        public const string NameSDKUpperCase = "GGEMCO";

        // 씬
        public const string PathScene = "Assets/"+NameSDK+"/Scenes";
        public const string SceneNamePreIntro = NameSDK+"_PreIntro";
        public const string SceneNameIntro = NameSDK+"_Intro";
        public const string SceneNameLoading = NameSDK+"_Loading";
        public const string SceneNameGame = NameSDK+"_Game";
        
        // 스파인 2d 사용 y/n
        public const string DefineSymbolSpine = NameSDKUpperCase+"_USE_SPINE";
        
        public const string DefineSymbolInputSystemOld = NameSDKUpperCase+"_USE_OLD_INPUT";
        public const string DefineSymbolInputSystemNew = NameSDKUpperCase+"_USE_NEW_INPUT";
        public const string DefineSymbolUseInGameTime = NameSDKUpperCase+"_USE_INGAME_TIME";
    }
}