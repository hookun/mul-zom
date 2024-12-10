using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static System.Net.Mime.MediaTypeNames;
using Text = UnityEngine.UI.Text;

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
    private GameObject whoami; //로컬 플레이어
    public float timeleft; //게임 시간
    public Text Timer; // 타이머 텍스트
    public Text restartTimer; //재시작 타이머 텍스트
    public int restartTime; //재시작 시간
    private bool isTimerRunning = false; //타이머 실행 확인용
    public bool isGameover { get; private set; } // 게임 오버 상태

    // 주기적으로 자동 실행되는, 동기화 메서드
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        // 로컬 오브젝트라면 쓰기 부분이 실행됨
        if (stream.IsWriting)
        {
            // 네트워크를 통해 score 값을 보내기
                 
            stream.SendNext((float)timeleft);
            stream.SendNext((int)restartTime);
        }
        else
        {
            // 리모트 오브젝트라면 읽기 부분이 실행됨         
            // 네트워크를 통해 score 값 받기
            
            timeleft = (float) stream.ReceiveNext();
            restartTime = (int) stream.ReceiveNext();
           
        }
        
    }
    


    private void Awake() {
        // 씬에 싱글톤 오브젝트가 된 다른 GameManager 오브젝트가 있다면
        

        isGameover = false;
        if (instance != this)
        {
            // 자신을 파괴
            Destroy(gameObject);
        }
        timeleft = 15f;
        restartTime = 5;
        print("웨이크");
    }

    // 게임 시작과 동시에 플레이어가 될 게임 오브젝트를 생성
  

    IEnumerator Start()
    {
        if (PhotonNetwork.InRoom)
        {
            Debug.LogWarning("클라이언트가 방에 있습니다.");
        }
        else
        {
            Debug.LogWarning("클라이언트가 방에 있지 않습니다.");
        }
        UIManager.instance.SetActiveGameoverUI(false);
        
        // 생성할 랜덤 위치 지정
        Vector3 randomSpawnPos = Random.insideUnitSphere * 5f;
        // 위치 y값은 0으로 변경
        randomSpawnPos.y = 0f;

        // 네트워크 상의 모든 클라이언트들에서 생성 실행
        // 단, 해당 게임 오브젝트의 주도권은, 생성 메서드를 직접 실행한 클라이언트에게 있음
        GameObject player = PhotonNetwork.Instantiate(playerPrefab.name, randomSpawnPos, Quaternion.identity);

        string msg = "\n<color=#00ff00>[" + PhotonNetwork.NickName + "] Connected" + "</color>";
        photonView.RPC("LogMsg", RpcTarget.AllBuffered, msg);

        FindWhoAmI(); //플레이어 이름 띄우기 위해 본인 캐릭터 검색

        yield return new WaitForSeconds(1.0f);
    }

    public void FindWhoAmI()//플레이어 이름 설정용
    {
        GameObject[] ppl = FindObjectsOfType<GameObject>();
        for(int i=0;i<ppl.Length;i++) { 
            if (ppl[i].tag=="Player"&&ppl[i].GetComponent<PhotonView>().IsMine) { 
                whoami = ppl[i]; 
                break; 
            } 
        }
        if (whoami != null) { print("로컬플레이어: " + whoami.name); } 
        else { print("로컬플레이어 못찾음."); }
    }
    // 점수를 추가하고 UI 갱신
    public void AddScore(int newScore) {//사용 안하나 혹시모를 에러 방지로 방치
        // 게임 오버가 아닌 상태에서만 점수 증가 가능
        if (!isGameover)
        {
            // 점수 추가
            score += newScore;
            // 점수 UI 텍스트 갱신
            UIManager.instance.UpdateScoreText(score);
        }
    }

    private bool isEndGameCalled = false;
     // 게임 오버 처리
     
    public string findBest()//게임 끝나고 가장 킬수 높은사람 서치
    {
        string best = "";
        int bestscore = 0;
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");  //플레이어 오브젝트만 찾음
        foreach (GameObject playerObject in players)
        {
            if (playerObject.GetComponent<PlayerHealth>().kill > bestscore) //최고기록 비교
            {
                bestscore= playerObject.GetComponent<PlayerHealth>().kill;
                best = playerObject.GetComponent<PhotonView>().Owner.NickName;
            }else if(playerObject.GetComponent<PlayerHealth>().kill == bestscore&&bestscore!=0) //중복시
            {
                best = best + ", "+playerObject.GetComponent<PhotonView>().Owner.NickName;
            }
            else
            {
                best = "Everybody is pacifist";
            }
        }
        best = best + " for " + bestscore + "kill";
        return best;
    }
    IEnumerator TimerCoroutine() //타이머
    {
        while (restartTime > 0)
        {
            yield return new WaitForSeconds(1); // 1초 대기

            restartTime -= 1;

            photonView.RPC("RestartTime", RpcTarget.All, restartTime); // 1초마다 방 모두에게 전달
        }

        print("타이머 종료");
    }

    [PunRPC]
    void RestartTime(int number)//클라이언트간 타이머 동기화
    {
        restartTimer.text = number.ToString(); //타이머 갱신
    }
    private bool isSceneLoading = false;

    [PunRPC]
    public void EndGame()//게임 종료(라운드 종료)시
    {
        if (isEndGameCalled)
        {
            return;
        }

        // 게임 오버 메시지 표시
        UIManager.instance.showWhoisBest(findBest());
        UIManager.instance.SetActiveGameoverUI(true);

        // EndGame이 호출되었음을 표시
        isEndGameCalled = true;
        
        whoami.GetComponent<PlayerInput>().enabled = false;
        print("십라");
        // 모든 클라이언트에 플래그 상태를 동기화
        photonView.RPC("SyncEndGameFlag", RpcTarget.OthersBuffered, isEndGameCalled);
    }

    [PunRPC]
    void SyncEndGameFlag(bool endGameFlag)
    {
        isEndGameCalled = endGameFlag;
    }

    // Update 메서드 수정

   
    private void Update()
    {

        if (PhotonNetwork.IsMasterClient)
        {
            if (Mathf.FloorToInt(timeleft) > 0) //타이머 남은시간 체크
            {
                timeleft -= Time.deltaTime;
                photonView.RPC("ShowTimer", RpcTarget.All, timeleft);
            }
            else
            {

                if (!isEndGameCalled) // EndGame이 한 번만 호출되도록 방지
                {

                    photonView.RPC("EndGame", RpcTarget.All);
                    isEndGameCalled = true;
                    photonView.RPC("SyncEndGameFlag", RpcTarget.OthersBuffered, isEndGameCalled);
                }

                photonView.RPC("RestartTime", RpcTarget.All, restartTime);
                if (!isTimerRunning) // 타이머가 이미 실행 중인지 확인
                {
                    StartCoroutine(TimerCoroutine());
                    isTimerRunning = true;
                }
                if (restartTime == 0 && !isSceneLoading)
                {
                    photonView.RPC("Restart", RpcTarget.All);
                }
            }
        }
        else if (!PhotonNetwork.IsMasterClient) //재시작 타이머 작동시 접속할때
        {
            
            if (Mathf.FloorToInt(timeleft) == 0) //타이머 남은시간 체크
            {
                UIManager.instance.showWhoisBest(findBest());
                UIManager.instance.SetActiveGameoverUI(true);
                
                
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape)) //ESC누르면 방 나가게끔
        {
            PhotonNetwork.LeaveRoom();
        }

        if (ChatInput.isFocused) //채팅 입력시 캐릭터 안움직이게끔 / 원래는 그냥 wasd입력하면 채팅입력중에도 움직임
        {
            if (!photonView.IsMine) return;
            else if (photonView.IsMine)
            {
                whoami.GetComponent<PlayerMovement>().enabled = false;
            }
        }
        else//채팅 끝나면 다시 움직이게끔
            whoami.GetComponent<PlayerMovement>().enabled = true;

        if (Input.GetKeyDown(KeyCode.Return) && !ChatInput.isFocused) //채팅발송
        {
            Send();
        }
    }
    public void Send() //채팅틀
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
    void ChatRPC(string msg)//채팅 발송
    {

        txtLogMsg.text += msg;

    }
    public void OnClickExitRoom() //퇴장알림
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
    [PunRPC]
    void ShowTimer(float number)//타이머 텍스트 표시
    {
        Timer.text = "TIME LEFT : " + Mathf.FloorToInt(number).ToString();
    }
    [PunRPC]
    void Restart()//재시작
    {
        Debug.Log("MasterClient is restarting the scene.");

        

        if (!isSceneLoading)
        {
            isSceneLoading = true; // 씬 로드 시작 플래그 설정
            if (PhotonNetwork.IsMasterClient)
            {
                
                
                PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer.ActorNumber);
                
                Debug.Log($"Player {PhotonNetwork.LocalPlayer.NickName}의 오브젝트를 삭제했습니다.");
            }
           
            PhotonNetwork.LoadLevel("Main");

        }
    }
}