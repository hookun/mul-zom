using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 점수와 게임 오버 여부, 게임 UI를 관리하는 게임 매니저
public class GameManager : MonoBehaviourPunCallbacks, IPunObservable {
    // 외부에서 싱글톤 오브젝트를 가져올때 사용할 프로퍼티
    public static GameManager instance
    {
        get
        {
            // 만약 싱글톤 변수에 아직 오브젝트가 할당되지 않았다면
            if (m_instance == null)
            {
                // 씬에서 GameManager 오브젝트를 찾아 할당
                m_instance = FindObjectOfType<GameManager>();
            }

            // 싱글톤 오브젝트를 반환
            return m_instance;
        }
    }

    private static GameManager m_instance; // 싱글톤이 할당될 static 변수

    public GameObject playerPrefab; // 생성할 플레이어 캐릭터 프리팹
    public Text txtLogMsg;
    public InputField ChatInput;
    private int score = 0; // 현재 게임 점수
    private GameObject whoami;
    public bool isGameover { get; private set; } // 게임 오버 상태

    // 주기적으로 자동 실행되는, 동기화 메서드
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        // 로컬 오브젝트라면 쓰기 부분이 실행됨
        if (stream.IsWriting)
        {
            // 네트워크를 통해 score 값을 보내기
            stream.SendNext(score);
        }
        else
        {
            // 리모트 오브젝트라면 읽기 부분이 실행됨         

            // 네트워크를 통해 score 값 받기
            score = (int) stream.ReceiveNext();
            // 동기화하여 받은 점수를 UI로 표시
            UIManager.instance.UpdateScoreText(score);
        }
    }


    private void Awake() {
        // 씬에 싱글톤 오브젝트가 된 다른 GameManager 오브젝트가 있다면
        
        if (instance != this)
        {
            // 자신을 파괴
            Destroy(gameObject);
        }
    }

    // 게임 시작과 동시에 플레이어가 될 게임 오브젝트를 생성
    IEnumerator Start() {
        
        // 생성할 랜덤 위치 지정
        Vector3 randomSpawnPos = Random.insideUnitSphere * 5f;
        // 위치 y값은 0으로 변경
        randomSpawnPos.y = 0f;
        
        // 네트워크 상의 모든 클라이언트들에서 생성 실행
        // 단, 해당 게임 오브젝트의 주도권은, 생성 메서드를 직접 실행한 클라이언트에게 있음
        PhotonNetwork.Instantiate(playerPrefab.name, randomSpawnPos, Quaternion.identity);
        string msg = "\n<color=#00ff00>[" + PhotonNetwork.NickName + "] Connected" + "</color>";
        photonView.RPC("LogMsg", RpcTarget.AllBuffered, msg);
        FindWhoAmI();
        yield return new WaitForSeconds(1.0f);
    }
    public void FindWhoAmI()
    {
        GameObject[] ppl = FindObjectsOfType<GameObject>();
        for(int i=0;i<ppl.Length;i++) { 
            if (ppl[i].tag=="Player"&&ppl[i].GetComponent<PhotonView>().IsMine) { 
                whoami = ppl[i]; 
                break; 
            } 
        }
        if (whoami != null) { Debug.Log("Local player object found: " + whoami.name); } 
        else { Debug.LogWarning("Local player object not found."); }
    }
    // 점수를 추가하고 UI 갱신
    public void AddScore(int newScore) {
        // 게임 오버가 아닌 상태에서만 점수 증가 가능
        if (!isGameover)
        {
            // 점수 추가
            score += newScore;
            // 점수 UI 텍스트 갱신
            UIManager.instance.UpdateScoreText(score);
        }
    }

    // 게임 오버 처리
    public void EndGame() {
        // 게임 오버 상태를 참으로 변경
        isGameover = true;
        // 게임 오버 UI를 활성화
        UIManager.instance.SetActiveGameoverUI(true);
    }

    // 키보드 입력을 감지하고 룸을 나가게 함
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PhotonNetwork.LeaveRoom();
        }

        if (ChatInput.isFocused)
        {
            if (!photonView.IsMine) return;
            else if (photonView.IsMine)
            {
                whoami.GetComponent<PlayerMovement>().enabled = false;
            }
        }
        else
            whoami.GetComponent<PlayerMovement>().enabled = true;
        
        if (Input.GetKeyDown(KeyCode.Return) && !ChatInput.isFocused)
        {
            Send();
        }
    }
    public void Send()
    {
        //채팅 전송할 때 인풋필드 내용이 있는지 검사, 없다면 무시
        if (!string.IsNullOrEmpty(ChatInput.text))
        {
            string chatText = "\n<color=#00ff00>[" +
                PhotonNetwork.NickName + "] : " + ChatInput.text +
                "</color>";
            photonView.RPC("ChatRPC", RpcTarget.AllBuffered, chatText);
            ChatInput.text = "";
        }
        //채팅내용을 전송한 뒤 인풋필드 레이블을 비워주고,
        //포커스를 다시 인풋필드에 맞춤
        ChatInput.ActivateInputField();
    }
    [PunRPC]
    void ChatRPC(string msg)
    {

        txtLogMsg.text += msg;

    }
    public void OnClickExitRoom()
    {

        string msg = "\n<color=#ff0000>[" + PhotonNetwork.NickName + "] Disconnected" + "</color>";
        photonView.RPC("LogMsg", RpcTarget.AllBuffered, msg);

        PhotonNetwork.LeaveRoom();
    }
    // 룸을 나갈때 자동 실행되는 메서드
    public override void OnLeftRoom() {
        // 룸을 나가면 로비 씬으로 돌아감
        SceneManager.LoadScene("Lobby");
    }
    [PunRPC]
    void LogMsg(string msg) //로그 메시지 Text UI에 텍스트를 누적시켜 표시
    {
        txtLogMsg.text = txtLogMsg.text + msg;
    }
}