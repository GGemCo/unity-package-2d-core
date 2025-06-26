namespace GGemCo2DCore
{
    public class ConfigDefine
    {
        public const string NameSDK = "GGemCo";
        public const string NameSDKUpperCase = "GGEMCO";
        
        public const string NamePackageCore = "Core";
        public const string NamePackagePlatformer = "Platformer";
        
        // 씬
        public const string PathScene = "Assets/"+NameSDK+"/Scenes";
        public const string SceneNameIntro = NameSDK+"_Intro";
        public const string SceneNameLoading = NameSDK+"_Loading";
        public const string SceneNameGame = NameSDK+"_Game";
        
        // 스파인 2d 사용 y/n
        public const string DefineSymbolSpine = NameSDKUpperCase+"_USE_SPINE";
        
        public const string DefineSymbolInputSystemOld = NameSDKUpperCase+"_USE_OLD_INPUT";
        public const string DefineSymbolInputSystemNew = NameSDKUpperCase+"_USE_NEW_INPUT";
    }
}