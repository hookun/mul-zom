using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using UnityEngine;

public static class CustomSerializationHelper
{
    // Photon이 `Color`를 전송할 수 있도록 등록
    public static void RegisterCustomTypes()
    {
        PhotonPeer.RegisterType(typeof(Color), 255, SerializeColor, DeserializeColor);
    }

    // Color -> byte[] 변환 (직렬화)
    private static byte[] SerializeColor(object obj)
    {
        Color color = (Color)obj;
        byte[] bytes = new byte[16]; // float 4개 * 4바이트
        int index = 0;

        Protocol.Serialize(color.r, bytes, ref index);
        Protocol.Serialize(color.g, bytes, ref index);
        Protocol.Serialize(color.b, bytes, ref index);
        Protocol.Serialize(color.a, bytes, ref index);

        return bytes;
    }

    // byte[] -> Color 변환 (역직렬화)
    private static object DeserializeColor(byte[] data)
    {
        Color color = new Color();
        int index = 0;

        Protocol.Deserialize(out color.r, data, ref index);
        Protocol.Deserialize(out color.g, data, ref index);
        Protocol.Deserialize(out color.b, data, ref index);
        Protocol.Deserialize(out color.a, data, ref index);

        return color;
    }
}
