using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class FindGameService : MonoBehaviour
{
    public class CAppInfo
    {
        public string AppName;
        public string PackageName;
        public bool IsGame;
        public bool IsDone;
        public bool IsDoubtGame;

        public CAppInfo(string aAppName, string aPackageName)
        {
            AppName = aAppName;
            PackageName = aPackageName;
            IsGame = false;
            IsDone = false;
            IsDoubtGame = false;
        }

        public override string ToString()
        {
            return $"AppName={AppName}, PackageName={PackageName}, IsGame={IsGame}, IsDoubtGame={IsDoubtGame}";
        }
    }

    public Button vFindButton;
    public Button vWebRequestTestButton;
    public Transform vContentList;
    public Text vStatusText;
    public Text vAPIText;
    public string vTestURL = "";

    private List<CAppInfo> mAppInfoList = new List<CAppInfo>(1024);
    private GameObject mPrefabAppInfo = null;
    private int mAPILevel = 0;

    void Awake()
    {
        Debug.Assert(vFindButton != null);
        Debug.Assert(vWebRequestTestButton != null);
        Debug.Assert(vContentList != null);
        Debug.Assert(vStatusText != null);
        Debug.Assert(vAPIText != null);

        mAPILevel = getSDKInt();
        mPrefabAppInfo = Resources.Load<GameObject>("AppInfo");
        vFindButton.onClick.AddListener(OnClickFindButton);
        vWebRequestTestButton.onClick.AddListener(OnClickWebRequestTestButton);
        vStatusText.text = "-준비-";
        vAPIText.text = mAPILevel.ToString();
    }

    public void OnClickFindButton()
    {
        vStatusText.text = "-검사중-";
        // 내 디바이스에 설치된 패키지를 전부 얻어온다.
        _FindPackageNames();

        // 얻어온 패키지 리스트를 기반으로 웹에 요청한다.
        StartCoroutine(_CoFindApp());
    }

    public void OnClickWebRequestTestButton()
    {
        StartCoroutine(_CoWebRequestTest());
    }

    private IEnumerator _CoFindApp()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        foreach (var lAppInfo in mAppInfoList)
        {
            yield return StartCoroutine(_WebRequest(lAppInfo));
        }
        

        while (mAppInfoList.All(d => d.IsDone) == false)
        {
            yield return null;
        }
#endif

#if UNITY_EDITOR
        for (int i = 0; i < 10; i++)
        {
            CAppInfo lAppInfo = new CAppInfo(i.ToString(), "");
            lAppInfo.IsDoubtGame = true;
            mAppInfoList.Add(lAppInfo);
        }

        yield return null;
#endif

        // 후처리
        foreach (var lAppInfo in mAppInfoList)
        {
            if (lAppInfo.IsDoubtGame)
            {
                Debug.Log(lAppInfo.ToString());
                GameObject lInstantiateObject = Instantiate(mPrefabAppInfo, vContentList);
                UIAppInfo lUIAppInfo = lInstantiateObject.GetComponent<UIAppInfo>();
                lUIAppInfo.SetAppNameText($"{lAppInfo.AppName} / {lAppInfo.PackageName}");
            }
        }

        vStatusText.text = "-던-";
        Debug.Log("-던-");
    }

    private IEnumerator _CoWebRequestTest()
    {
        string lPackageName = vTestURL;
        CAppInfo lAppInfo = new CAppInfo("", lPackageName);
        yield return _WebRequest(lAppInfo);

        Debug.Log(lAppInfo.ToString());
    }

    private void _FindPackageNames()
    {
        Debug.Log("_FindPackageNames");

#if UNITY_ANDROID && !UNITY_EDITOR
        string[] appNames;
        string[] packageNames;

        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = jc.GetStatic<AndroidJavaObject>("currentActivity");
        int flag = new AndroidJavaClass("android.content.pm.PackageManager").GetStatic<int>("GET_META_DATA");
        AndroidJavaObject pm = currentActivity.Call<AndroidJavaObject>("getPackageManager");
        AndroidJavaObject packages = pm.Call<AndroidJavaObject>("getInstalledApplications", flag);

        int count = packages.Call<int>("size");
        //AndroidJavaObject[] links = new AndroidJavaObject[count];
        appNames = new string[count];
        packageNames = new string[count];


        int ii = 0;
        for (int i = 0; ii < count;)
        {
            AndroidJavaObject currentObject = packages.Call<AndroidJavaObject>("get", ii);
            try
            {
                appNames[i] = pm.Call<string>("getApplicationLabel", currentObject);
                packageNames[i] = currentObject.Get<string>("packageName");
                string lInstallerPackageName = string.Empty;
                if (mAPILevel >= 30)
                {
                    AndroidJavaObject lInstallSourceInfo = pm.Call<AndroidJavaObject>("getInstallSourceInfo", packageNames[i]);
                    lInstallerPackageName = lInstallSourceInfo.Call<string>("getInitiatingPackageName");
                }
                else
                {
                    lInstallerPackageName = pm.Call<string>("getInstallerPackageName", packageNames[i]);
                }

                //Debug.Log($"lInstallerPackageName={lInstallerPackageName}");

                // 설치 된 출처를 구분한다. (ex: 구글 플레이 스토어, 원스토어, 삼성 스토어 등등)
                if (string.IsNullOrEmpty(lInstallerPackageName) == false &&
                    lInstallerPackageName.Contains("com.android.vending"))
                {
                    mAppInfoList.Add(new CAppInfo(appNames[i], packageNames[i]));
                }

                i++;
                ii++;
            }
            catch (System.Exception e)
            {
                Debug.Log(e.Message);
                //if it fails, just go to the next app and try to add to that same entry.
                Debug.Log("skipped " + ii);
                ii++;
            }
        }

        // for (int lCount = 0; lCount < count; lCount++)
        // {
        //     Debug.Log($"AppName={appNames[lCount]}, PackageName={packageNames[lCount]}");
        // }

        packages.Dispose();
        pm.Dispose();
        currentActivity.Dispose();
        jc.Dispose();
#endif
    }

    static int getSDKInt()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var version = new AndroidJavaClass("android.os.Build$VERSION")) {
            return version.GetStatic<int>("SDK_INT");
        }
#else
        return 0;
#endif
    }

    private IEnumerator _WebRequest(CAppInfo aAppInfo) //string aPackageName)
    {
        yield return StartCoroutine(_CoWebRequest(aAppInfo));
    }

    private IEnumerator _CoWebRequest(CAppInfo aAppInfo)
    {
        if (string.IsNullOrEmpty(aAppInfo.PackageName) == false)
        {
            string lBaseURL = "https://play.google.com/store/apps/details?id=";
            string lRequestURL = lBaseURL + aAppInfo.PackageName;
            UnityWebRequest lWebRequest = UnityWebRequest.Get(lRequestURL);
            yield return lWebRequest.SendWebRequest();
            if (string.IsNullOrEmpty(lWebRequest.error))
            {
                //Debug.Log(lWebRequest.downloadHandler.text);
                string lRawResultText = lWebRequest.downloadHandler.text;
                if (lRawResultText.Contains("<a itemprop=\"genre\" href=\"/store/apps/category/GAME_"))
                {
                    Debug.Log($"PackageName={aAppInfo.PackageName}");
                    aAppInfo.IsGame = true;
                }
                else
                {
                    Debug.Log($"NOT GAME={aAppInfo.PackageName}");
                    aAppInfo.IsGame = false;
                }

                // string lFilePath = Application.dataPath + "/WebResult.txt";
                // FileStream lFileStream = new FileStream(lFilePath, FileMode.Create);
                // StreamWriter lStreamWriter = new StreamWriter(lFileStream);
                // lStreamWriter.Write(lWebRequest.downloadHandler.text);
                // lStreamWriter.Close();
                // lFileStream.Close();
            }
            else
            {
                Debug.Log(lWebRequest.error);
                aAppInfo.IsDoubtGame = true;
            }
        }

        aAppInfo.IsDone = true;
    }
}