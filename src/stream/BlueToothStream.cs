using System;
using System.Text;
using System.Collections;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using InTheHand.Net;
using InTheHand.Net.Sockets;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Ports;

namespace hobd
{

public class BluetoothStream: IStream
{
    NetworkStream stream;
    BluetoothClient bluetoothClient;

    public BluetoothStream()
    {}

    public static string ConstructUrl(string url, string svc, string pin)
    {
        string str = "btspp://" + url;
        if (svc != null)
        {
            str = str + ":" + svc;
        }
        if (pin != null)
        {
            str = str + ";pin=" + pin;
        }
        return str;
    }

 /**
     * returns string array of {address, serviceid, pin}
     */
    public static string[] ParseUrl(string url)
    {
        Regex rx = new Regex(@"^btspp:// ([\d\w]+) (\:(\d+))? (\;pin=(\d+))? $", RegexOptions.IgnorePatternWhitespace);
        
        var match = rx.Match(url);
        
        if (match == null)
            return new string[]{null, null, null};
        
        var serviceid = match.Groups[3].Value;
        if (serviceid == "") serviceid = null;

        var pid = match.Groups[5].Value;
        if (pid == "") pid = null;

        return new string[]{ match.Groups[1].Value, serviceid, pid };
    }
    public const int URL_ADDR = 0;
    public const int URL_SVC = 1;
    public const int URL_PIN = 2;
    
    bool try_with_service = false;
    
    public void Open(String url)
    {
        try{
            
            var parsed_url = ParseUrl(url);
            Logger.trace("BluetoothStream", "Open " + parsed_url[URL_ADDR] + " serviceid " + parsed_url[URL_SVC] + " pin " + parsed_url[URL_PIN]);

            try{
                BluetoothRadio.PrimaryRadio.Mode = RadioMode.Discoverable;
            }catch(Exception e){
                Logger.error("BluetoothStream", "set_Mode", e);
            }

            BluetoothAddress address = BluetoothAddress.Parse(parsed_url[URL_ADDR]);

            bluetoothClient = new BluetoothClient();

            try{
                if (parsed_url[URL_PIN] != null)
                    bluetoothClient.SetPin(address, parsed_url[URL_PIN]);
            }catch(Exception){
                Logger.error("BluetoothStream", "SetPin");
            }
            BluetoothEndPoint btep;

            // force serviceid for some popular china BT adapters
            if (parsed_url[URL_SVC] == null && try_with_service) {
                parsed_url[URL_SVC] = "1";
            }
            
            if (parsed_url[URL_SVC] != null)
                btep = new BluetoothEndPoint(address, BluetoothService.SerialPort, int.Parse(parsed_url[URL_SVC]));
            else
                btep = new BluetoothEndPoint(address, BluetoothService.SerialPort);
            bluetoothClient.Connect(btep);
            stream = bluetoothClient.GetStream();
            if (stream == null){
                bluetoothClient.Close();
                bluetoothClient = null;
            }else{
                if (stream.CanTimeout){
                    stream.WriteTimeout = 2;
                    stream.ReadTimeout = 2;
                }
            }
        }catch(Exception e){
            if (bluetoothClient != null)
            {
                bluetoothClient.Close();
                bluetoothClient = null;
            }
            if (!try_with_service){
                try_with_service = true;
                Open(url);
                return;
            }
            throw e;
        }
    }
    
    public void Close()
    {
        if (bluetoothClient != null)
        {
            bluetoothClient.Close();
            bluetoothClient = null;
        }
        if (stream != null)
        {    
            stream.Close();
            stream = null;
        }
}
    
    public bool HasData()
    {
        if (bluetoothClient != null && stream != null)
        {
            try{
                return stream.DataAvailable;
            }catch(Exception){
                bluetoothClient = null;
                return false;
            }
        }
            return false;
    }
    
    public byte[] Read()
    {
        if (bluetoothClient != null)
        {
            byte[] buf = new byte[1024];
            int len;
            try{
                len = stream.Read(buf, 0, buf.Length);
            }catch(System.IO.IOException){
                bluetoothClient = null;
                return null;
            }
            byte[] outputData_ = new byte[len];
            Array.Copy(buf, 0, outputData_, 0, len);
            return outputData_;
        }
        return null;
    }
    
    public void Write(byte[] array, int offset, int length)
    {
        if (bluetoothClient != null && stream != null)
        {
            try{
                stream.Write(array, offset, length);
            }catch(Exception){
                bluetoothClient = null;
            }
        }
    }
}
}