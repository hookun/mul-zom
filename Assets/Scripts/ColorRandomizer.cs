using Photon.Pun;
using UnityEngine;

public class ColorRandomizer : MonoBehaviourPun, IPunObservable
{
    private Renderer playerRenderer;
    public float c1; // ���� r
    public float c2; // ���� g
    public float c3; // ���� b
    public Color randomColor;
    void Awake()
    {
        playerRenderer = GetComponentInChildren<Renderer>();
    }

    void Start()
    {
        if (photonView.IsMine)
        {
            
            // ���� ���� ����
            randomColor = new Color(Random.value, Random.value, Random.value);
            c1 = randomColor.r;
            c2 = randomColor.g;
            c3 = randomColor.b;

            // ��Ʈ��ũ�� ���� ���� ���� ������
            photonView.RPC("SetPlayerColor", RpcTarget.AllBuffered, c1, c2, c3);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // ���� ������Ʈ��� ���� ���� ������
            stream.SendNext(c1);
            stream.SendNext(c2);
            stream.SendNext(c3);
        }
        else
        {
            // ����Ʈ ������Ʈ��� ���� �� �ޱ�
            c1 = (float)stream.ReceiveNext();
            c2 = (float)stream.ReceiveNext();
            c3 = (float)stream.ReceiveNext();

            // ���� ���� ������ ĳ���� ���� ����
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
