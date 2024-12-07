using Photon.Pun;
using UnityEngine;

public class ColorRandomizer : MonoBehaviourPun, IPunObservable
{
    private Renderer playerRenderer;
    public float c1; // 색상 r
    public float c2; // 색상 g
    public float c3; // 색상 b
    public Color randomColor;
    void Awake()
    {
        playerRenderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        if (photonView.IsMine)
        {
            
            // 랜덤 색상 생성
            randomColor = new Color(Random.value, Random.value, Random.value);
            c1 = randomColor.r;
            c2 = randomColor.g;
            c3 = randomColor.b;

            // 네트워크를 통해 색상 값을 보내기
            photonView.RPC("SetPlayerColor", RpcTarget.AllBuffered, c1, c2, c3);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 로컬 오브젝트라면 색상 값을 보내기
            stream.SendNext(c1);
            stream.SendNext(c2);
            stream.SendNext(c3);
        }
        else
        {
            // 리모트 오브젝트라면 색상 값 받기
            c1 = (float)stream.ReceiveNext();
            c2 = (float)stream.ReceiveNext();
            c3 = (float)stream.ReceiveNext();

            // 받은 색상 값으로 캐릭터 색상 설정
            SetPlayerColor(c1, c2, c3);
        }
    }

    [PunRPC]
    void SetPlayerColor(float r, float g, float b)
    {
        Color color = new Color(r, g, b);

        if (playerRenderer != null)
        {
            
            playerRenderer.material.color = color;
        }
    }
}
