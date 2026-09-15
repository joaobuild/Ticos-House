using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

public static class BuildGame
{
    [MenuItem("Tico's House/Build Windows x64")]
    public static void Windows()
    {
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory("Assets/Resources");
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/PixelBackground.mat")==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Unlit/Texture")),"Assets/Resources/PixelBackground.mat");
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/PixelAgent.mat")==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Unlit/Transparent")),"Assets/Resources/PixelAgent.mat");
        foreach(string path in Directory.GetFiles("Assets/Resources/Art","*.png")){
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer!=null){importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
        }
        // An explicit material reference prevents Standard shader stripping in a procedural scene.
        if(AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/RuntimeStandard.mat")==null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),"Assets/Resources/RuntimeStandard.mat");
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        bool hasStandard=false;
        for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==Shader.Find("Standard"))hasStandard=true;
        if(!hasStandard){shaders.InsertArrayElementAtIndex(shaders.arraySize);shaders.GetArrayElementAtIndex(shaders.arraySize-1).objectReferenceValue=Shader.Find("Standard");graphics.ApplyModifiedProperties();}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Tico's House Runtime").AddComponent<TicosHouse.GameRuntime>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Night.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Night.unity",true)};
        PlayerSettings.companyName="Joaobuild";PlayerSettings.productName="Tico's House";
        PlayerSettings.bundleVersion="0.1.4";PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        PlayerSettings.runInBackground=true;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        PlayerSettings.SetApiCompatibilityLevel(UnityEditor.Build.NamedBuildTarget.Standalone,ApiCompatibilityLevel.NET_Standard);
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowDistance=28;
        AssetDatabase.SaveAssets();
        string output=System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"--qa-output")>=0?"Builds/QA/TicosHouse.exe":"Builds/TicosHouse/TicosHouse.exe";
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {scenes=new[]{"Assets/Scenes/Night.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
        if(report.summary.result!=BuildResult.Succeeded)throw new System.Exception("Build failed: "+report.summary.result);
        Debug.Log("TICOS_BUILD_SUCCESS "+report.summary.totalSize);
    }
}



