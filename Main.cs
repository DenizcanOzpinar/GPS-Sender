using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using UnityEngine.Android;


public class Main : MonoBehaviour{
    public InputField gps, battary, id;
    public Text sendedText;
    public bool isUpdating;
    private int width = 480;
    private int height = 800;
    private FullScreenMode fullScreenMode = FullScreenMode.ExclusiveFullScreen;
    private int refreshRate = 60;

    
private void Start(){
    Screen.SetResolution(width, height, fullScreenMode, refreshRate);
    id.text = "9000";
}

private void Update(){
     if (!isUpdating){
         StartCoroutine(LocationFinder());
         isUpdating = !isUpdating;
     }
    battary.text = GetBatteryLevel().ToString();
}


public void SendData(){
    try{
            Int32 port = 1023;
            TcpClient client = new TcpClient("5.188.155.41", port);
            NetworkStream stream = client.GetStream();
            String message = gps.text +",ID ="+ id.text +",Battery="+ battary.text;
            Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
            stream.Write(data, 0, data.Length);
        
            
            stream.Close();
            client.Close();
        }
        catch (ArgumentNullException e)
        {
            print(e.StackTrace);
            sendedText.text = e.Message;
        }
        catch (SocketException e)
        {
            print(e);
            sendedText.text = e.Message;
        }

        sendedText.gameObject.SetActive(true);
        StartCoroutine("TextDisapear");
    }

    IEnumerator TextDisapear(){
        yield return new WaitForSeconds(5);
        sendedText.gameObject.SetActive(false);
    }
    
    IEnumerator LocationFinder()
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation)){
            Permission.RequestUserPermission(Permission.FineLocation);
            Permission.RequestUserPermission(Permission.CoarseLocation);
        }
        
        // First, check if user has location service enabled
        if (!Input.location.isEnabledByUser)
            yield return new WaitForSeconds(3);

        // Start service before querying location
        Input.location.Start();

        // Wait until service initializes
        int maxWait = 7;
        while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
        {
            yield return new WaitForSeconds(1);
            maxWait--;
        }

        // Service didn't initialize in 20 seconds
        if (maxWait < 1)
        {
            print("Timed out");
            gps.text = " timed out";
            isUpdating = !isUpdating;
            yield break;
        }

        // Connection has failed
        if (Input.location.status == LocationServiceStatus.Failed)
        {
            print("Unable to determine device location");
            gps.text = "Unable to determine device location" ;
            isUpdating = !isUpdating;
            yield break;
        }
        else
        {
            gps.text = ConvertUnityLocationToNMEA(Input.location.lastData);
        }
        isUpdating = !isUpdating;
        Input.location.Stop();
    }
    
    private int GetBatteryLevel()
    {
        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                if (null != unityPlayer)
                {
                    using (AndroidJavaObject currActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                    {
                        if (null != currActivity)
                        {
                            using (AndroidJavaObject intentFilter = new AndroidJavaObject("android.content.IntentFilter", new object[] { "android.intent.action.BATTERY_CHANGED" }))
                            {
                                using (AndroidJavaObject batteryIntent = currActivity.Call<AndroidJavaObject>("registerReceiver", new object[] { null, intentFilter }))
                                {
                                    int level = batteryIntent.Call<int>("getIntExtra", new object[] { "level", -1 });
                                    int scale = batteryIntent.Call<int>("getIntExtra", new object[] { "scale", -1 });
                                    
                                    if (level == -1 || scale == -1)
                                    {
                                        return 50;
                                    }

                                    return (int)(((float)level / (float)scale) * 100.0f);
                                }
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            print(ex.Message);
        }

        return 100;
    }
    private static string ConvertUnityLocationToNMEA(LocationInfo locInfo){
        // Format the latitude and longitude in the NMEA protocol format
        string lat = locInfo.latitude.ToString("0.0000");
        string latHemi = (locInfo.latitude < 0.0) ? "S" : "N";
        string lon = locInfo.longitude.ToString("0.0000");
        string lonHemi = (locInfo.longitude < 0.0) ? "W" : "E";
        string nmeaData = string.Format("$GPGGA,{0:HHmmss.fff},{1},{2},{3},{4},{5:G},{6:G},1.0,0.0,M,0.0,M,,",
            DateTime.UtcNow, lat, latHemi, lon, lonHemi, locInfo.horizontalAccuracy, locInfo.altitude);

        // Calculate the NMEA checksum and append it to the string
        int checksum = 0;
        foreach (char c in nmeaData)
        {
            checksum ^= (int)c;
        }
        nmeaData += string.Format("*{0:X2}\r\n", checksum);

        return nmeaData;
    }
    
}
