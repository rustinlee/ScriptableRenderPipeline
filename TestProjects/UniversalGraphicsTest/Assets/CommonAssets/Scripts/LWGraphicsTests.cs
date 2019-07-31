using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.XR;
using UnityEngine.TestTools.Graphics;
using UnityEngine.SceneManagement;

public class LWGraphicsTests
{

    public const string lwPackagePath = "Assets/ReferenceImages";

    [UnityTest, Category("LightWeightRP")]
    [PrebuildSetup("SetupGraphicsTestCases")]
    [UseGraphicsTestCases(lwPackagePath)]


    public IEnumerator Run(GraphicsTestCase testCase)
    {
        SceneManager.LoadScene(testCase.ScenePath);

        // Always wait one frame for scene load
        yield return null;

        var cameras = GameObject.FindGameObjectsWithTag("MainCamera").Select(x=>x.GetComponent<Camera>());
        var settings = Object.FindObjectOfType<LWGraphicsTestSettings>();
        Assert.IsNotNull(settings, "Invalid test scene, couldn't find LWGraphicsTestSettings");

        Scene scene = SceneManager.GetActiveScene();

        if (scene.name.Substring(3, 4).Equals("_xr_"))
        {
            Assume.That((Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.OSXPlayer), "Stereo LWRP tests do not run on MacOSX.");

            XRSettings.LoadDeviceByName("MockHMD");
            yield return null;

            XRSettings.enabled = true;
            yield return null;

            XRSettings.gameViewRenderMode = GameViewRenderMode.BothEyes;
            yield return null;

            foreach (var camera in cameras)
                camera.stereoTargetEye = StereoTargetEyeMask.Both;
        }
        else
        {
            XRSettings.enabled = false;
            yield return null;
        }

        for (int i = 0; i < settings.WaitFrames; i++)
            yield return null;

        ImageAssert.AreEqual(testCase.ReferenceImage, cameras.Where(x => x != null), settings.ImageComparisonSettings);

#if CHECK_ALLOCATIONS_WHEN_RENDERING
        // Does it allocate memory when it renders what's on the main camera?
        bool allocatesMemory = false;
        var mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        try
        {
            ImageAssert.AllocatesMemory(mainCamera, 512, 512); // 512 used for height and width to render
        }
        catch (AssertionException)
        {
            allocatesMemory = true;
        }
        if (allocatesMemory)
            Assert.Fail("Allocated memory when rendering what is on main camera");
#endif

    }

#if UNITY_EDITOR
    [TearDown]
    public void DumpImagesInEditor()
    {
        UnityEditor.TestTools.Graphics.ResultsUtility.ExtractImagesFromTestProperties(TestContext.CurrentContext.Test);
    }
#endif
}
