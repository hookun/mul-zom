using ExitGames.Client.Photon;
using Photon.Pun; // 유니티용 포톤 컴포넌트들
using Photon.Realtime; // 포톤 서비스 관련 라이브러리
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 마스터(매치 메이킹) 서버와 룸 접속을 담당
public class LobbyManager : MonoBehaviourPunCallbacks {
    private string gameVersion = "1"; // 게임 버전
    
    public Text connectionInfoText; // 네트워크 정보를 표시할 텍스트
    public Button joinButton; // 룸 접속 버튼
    public Button makeRoomButton; // 룸 접속 버튼
    public InputField userId;
    public InputField roomName;

    public GameObject roomItem; // 룸 목록만큼 생성될 프리팹
    public GameObject scrollContents;  //RoomItem이 차일드로 생성될 Parent 객체

    private Dictionary<string, GameObject> rooms = new Dictionary<string, GameObject>();
    // 게임 실행과 동시에 마스터 서버 접속 시도
    private void Start() {
        

        // 접속에 필요한 정보(게임 버전) 설정
        PhotonNetwork.GameVersion = gameVersion;
        // 설정한 정보를 가지고 마스터 서버 접속 시도
        PhotonNetwork.ConnectUsingSettings();

        // 룸 접속 버튼을 잠시 비활성화
        joinButton.interactable = false;
        makeRoomButton.interactable = false;

        // 접속을 시도 중임을 텍스트로 표시
        connectionInfoText.text = "마스터 서버에 접속중...";
        userId.text = "Player" + Random.Range(0, 999).ToString("000"); //랜덤하게 이름 설정
    }

    // 마스터 서버 접속 성공시 자동 실행
    public override void OnConnectedToMaster() {
        PhotonNetwork.JoinLobby();
        // 룸 접속 버튼을 활성화
        joinButton.interactable = true;
        makeRoomButton.interactable = true;
        // 접속 정보 표시
        connectionInfoText.text = "온라인 : 마스터 서버와 연결됨";
        
    }

    // 마스터 서버 접속 실패시 자동 실행
    public override void OnDisconnected(DisconnectCause cause) {
        // 룸 접속 버튼을 비활성화
        joinButton.interactable = false;
        // 접속 정보 표시
        connectionInfoText.text = "오프라인 : 마스터 서버와 연결되지 않음\n접속 재시도 중...";

        // 마스터 서버로의 재접속 시도
        PhotonNetwork.ConnectUsingSettings();
    }
    public override void OnJoinedLobby()
    {

        Debug.Log("enter");
    }

    // 룸 접속 시도
    public void Connect() {
        // 중복 접속 시도를 막기 위해, 접속 버튼 잠시 비활성화
        joinButton.interactable = false;
        makeRoomButton.interactable = false;

        // 마스터 서버에 접속중이라면
        if (PhotonNetwork.IsConnected)
        {
            // 룸 접속 실행
            connectionInfoText.text = "룸에 접속...";

            string userName = userId.text;
            PhotonNetwork.NickName = userName;//유저 이름 설정
            PhotonNetwork.JoinRandomRoom();
            
        }
        else
        {
            // 마스터 서버에 접속중이 아니라면, 마스터 서버에 접속 시도
            connectionInfoText.text = "오프라인 : 마스터 서버와 연결되지 않음\n접속 재시도 중...";
            // 마스터 서버로의 재접속 시도
            PhotonNetwork.ConnectUsingSettings();
        }
    }
    
    // (빈 방이 없어)랜덤 룸 참가에 실패한 경우 자동 실행
    public override void OnJoinRandomFailed(short returnCode, string message) {
        // 접속 상태 표시
        connectionInfoText.text = "빈 방이 없음, 새로운 방 생성...";
        // 최대 4명을 수용 가능한 빈방을 생성
        OnClickCreateRoom();
    }

    // 룸에 참가 완료된 경우 자동 실행
    public override void OnJoinedRoom() {
        // 접속 상태 표시
        connectionInfoText.text = "방 참가 성공";
        
        // 모든 룸 참가자들이 Main 씬을 로드하게 함
        PhotonNetwork.LoadLevel("Main");

    }
    public void OnClickJoinRandomRoom() //Join Random Room 버튼 연결 함수
    {
        PhotonNetwork.NickName = userId.text; //로컬플레이어 이름 설정
        PlayerPrefs.SetString("USER_ID", userId.text); //플레이어 이름을 저장
        PhotonNetwork.JoinRandomRoom();//무작위 방 입장
    }
    public void OnClickCreateRoom() //Make Room 버튼 연결 함수
    {
        string _roomName = roomName.text; //사용자가 입력 방제목을 얻어옴
        if (string.IsNullOrEmpty(roomName.text)) // 사용자가 방제목을 입력 안했다면
        {
            _roomName = "Room_" + Random.Range(0, 999).ToString("000"); //랜덤하게 방제목 설정
        }
        PhotonNetwork.NickName = userId.text; //로컬 플레이어 이름 설정
        PlayerPrefs.SetString("USER_ID", userId.text);

        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = 4;

        PhotonNetwork.CreateRoom(_roomName, roomOptions, TypedLobby.Default);
    }
    public override void OnCreateRoomFailed(short returnCode, string message) //방만들기 실패 했을 때 호출되는 콜백함수
    {
        Debug.Log("방 만들기 실패: " + message);
    }
    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        print("룸 업데이트");
        // 삭제된 RoomItem 프리팹을 저장할 임시변수
        GameObject tempRoom = null;
        
        foreach (var roomInfo in roomList)
        {
            if (roomInfo.RemovedFromList == true)
            {  // 룸이 삭제된 경우
                // 딕셔너리에서 룸 이름으로 검색해 저장된 RoomItem 프리팹를 추출
                rooms.TryGetValue(roomInfo.Name, out tempRoom);
                Destroy(tempRoom); // RoomItem 프리팹 삭제        
                rooms.Remove(roomInfo.Name); // 딕셔너리에서 해당 룸 이름의 데이터를 삭제
                                             //GridLayoutGroup의 constraintCount 값을 RoomItem 갯수만큼 증가
                scrollContents.GetComponent<GridLayoutGroup>().constraintCount = rooms.Count;
                scrollContents.GetComponent<RectTransform>().sizeDelta -= new Vector2(0, 20);
            }
            else // 룸 정보가 변경된 경우
            {
                // 룸 이름이 딕셔너리에 없는 경우 새로 추가
                if (rooms.ContainsKey(roomInfo.Name) == false)
                {
                    GameObject room = (GameObject)Instantiate(roomItem); //RoomItem 프리팹 동적 생성
                    room.transform.SetParent(scrollContents.transform, false); // RoomItem을 scrollContents의 자식으로 설정

                    RoomData roomData = room.GetComponent<RoomData>();
                    roomData.roomName = roomInfo.Name; //방제목
                    roomData.connectPlayer = roomInfo.PlayerCount; //현재인원수
                    roomData.maxPlayer = roomInfo.MaxPlayers; //최대인원수
                    roomData.DispRoomData();//텍스트 정보 표시

                    roomData.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate
                    {
                        OnClickRoomItem(roomData.roomName);
                    });

                    // 딕셔너리 자료형에 데이터 추가
                    rooms.Add(roomInfo.Name, room);
                    //GridLayoutGroup의 constraintCount 값을 RoomItem 갯수만큼 증가
                    scrollContents.GetComponent<GridLayoutGroup>().constraintCount = rooms.Count;
                    //스크롤 영역의 높이를 증가
                    scrollContents.GetComponent<RectTransform>().sizeDelta += new Vector2(0, 20);
                }
                else // 룸 이름이 딕셔너리에 없는 경우에 룸 정보를 갱신
                {
                    rooms.TryGetValue(roomInfo.Name, out tempRoom); //룸검색해 RoomItem을 tempRoom에 저장

                    RoomData roomData = tempRoom.GetComponent<RoomData>();
                    roomData.roomName = roomInfo.Name; //방제목
                    roomData.connectPlayer = roomInfo.PlayerCount; //현재인원수
                    roomData.maxPlayer = roomInfo.MaxPlayers; //최대인원수
                    roomData.DispRoomData();//텍스트 정보 표시
                }
            }
        }
    }
    void OnClickRoomItem(string roomName)
    {
        PhotonNetwork.NickName = userId.text; //방 접속한 플레이어 이름 설정
        PlayerPrefs.SetString("USER_ID", userId.text); // 플레이어 이름 저장
        PhotonNetwork.JoinRoom(roomName); // 방 제목으로 방 입장
    }
}