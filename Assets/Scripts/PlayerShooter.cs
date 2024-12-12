using Photon.Pun;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// 주어진 Gun 오브젝트를 쏘거나 재장전
// 알맞은 애니메이션을 재생하고 IK를 사용해 캐릭터 양손이 총에 위치하도록 조정
public class PlayerShooter : MonoBehaviourPun {

    public List<Gun> gunList = new List<Gun>();//총기
    public List<Transform> lefthandlist = new List<Transform>();//왼팔
    public List<Transform> righthandlist = new List<Transform>();//오른팔
    public int gunNumber=0; // 중도 참여 시 총기 종류 동기화용
    public Gun gun; // 사용할 총
    public Transform gunPivot; // 총 배치의 기준점
    public Transform leftHandMount; // 총의 왼쪽 손잡이, 왼손이 위치할 지점
    public Transform rightHandMount; // 총의 오른쪽 손잡이, 오른손이 위치할 지점
    public Gun preGun;//이전 총
    private PlayerInput playerInput; // 플레이어의 입력
    private Animator playerAnimator; // 애니메이터 컴포넌트

    private Dictionary<int, (int magAmmo, int ammoRemain)> ammoData; // 무기의 탄약 정보를 저장하는 Dictionary

    private void Start() {
        // 사용할 컴포넌트들을 가져오기
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();

        ammoData = new Dictionary<int, (int, int)>();

        
        gun = gunList[0];
        

    }
    private void Awake() //start에서 실행시 동기화 때 문제생겨서 Awake에서 생성
    {
        ammoData = new Dictionary<int, (int, int)>();
        foreach (var g in gunList)
        {
            ammoData[g.gunID] = (g.magAmmo, g.ammoRemain);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        // 로컬 오브젝트라면 쓰기 부분이 실행됨
        if (stream.IsWriting)
        {
            // 네트워크를 통해 score 값을 보내기
            stream.SendNext(gunNumber);
        }
        else
        {
            // 리모트 오브젝트라면 읽기 부분이 실행됨         
            
            // 네트워크를 통해 값 받기
            gunNumber = (int)stream.ReceiveNext();
            
            SwitchWeapon(gunNumber);
            photonView.RPC("SwitchWeapon", RpcTarget.OthersBuffered, gunNumber);

        }
    }
    private void OnEnable() {
        // 슈터가 활성화될 때 총도 함께 활성화
        gun.gameObject.SetActive(true);
        
        
    }

    private void OnDisable() {
        // 슈터가 비활성화될 때 총도 함께 비활성화
        gun.gameObject.SetActive(false);
    }

    private void Update() {
        // 로컬 플레이어만 총을 직접 사격, 탄약 UI 갱신 가능
        if (!photonView.IsMine)
        {
            return;
        }

        // 입력을 감지하고 총 발사하거나 재장전
        
        if (playerInput.weapon1)
        {
            gunNumber = 0;
            SwitchWeapon(gunNumber);
            photonView.RPC("SwitchWeapon", RpcTarget.OthersBuffered,gunNumber);
        }
        else if (playerInput.weapon2)
        {
            gunNumber = 1;
            SwitchWeapon(gunNumber);
            photonView.RPC("SwitchWeapon", RpcTarget.OthersBuffered, gunNumber);
        }
        else if (playerInput.weapon3)
        {
            gunNumber = 2;
            SwitchWeapon(gunNumber);
            photonView.RPC("SwitchWeapon", RpcTarget.OthersBuffered, gunNumber);
        }
        if (!gun.isSingleOnly)
        {
            if (playerInput.fire)
            {
                gun.Fire();
            }
            else if (playerInput.reload)
            {
                if (gun.Reload())
                {
                    playerAnimator.SetTrigger("Reload");
                }
            }
        }
        else if (gun.isSingleOnly)
        {
            if (playerInput.singlefire)
            {
                gun.Fire();
            }
            else if (playerInput.reload)
            {
                if (gun.Reload())
                {
                    playerAnimator.SetTrigger("Reload");
                }
            }
        }
        else if (playerInput.reload)
        {
            // 재장전 입력 감지시 재장전
            if (gun.Reload())
            {
                // 재장전 성공시에만 재장전 애니메이션 재생
                playerAnimator.SetTrigger("Reload");
            }
        }

        // 남은 탄약 UI를 갱신
        UpdateUI();
    }
    [PunRPC]
    private void SwitchWeapon(int index)
    {
        if (index < 0 || index >= gunList.Count) return;

        if (gun != null)
        {


            // 현재 무기의 탄약 정보 저장
            ammoData[gun.gunID] = (gun.magAmmo, gun.ammoRemain);
            gun.gameObject.SetActive(false);
        }
        

        gun = gunList[index];
        gun.gameObject.SetActive(true);

        // 무기의 탄약 정보 복원
        if (ammoData.TryGetValue(gun.gunID, out var ammoInfo))
        {
            gun.magAmmo = ammoInfo.magAmmo;
            gun.ammoRemain = ammoInfo.ammoRemain;
        }

        leftHandMount = lefthandlist[index];
        rightHandMount = righthandlist[index];
        UpdateUI();
    }
    // 탄약 UI 갱신
    private void UpdateUI() {
        if (gun != null && UIManager.instance != null)
        {
            // UI 매니저의 탄약 텍스트에 탄창의 탄약과 남은 전체 탄약을 표시
            UIManager.instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
        }
    }

    // 애니메이터의 IK 갱신
    private void OnAnimatorIK(int layerIndex) {
        // 총의 기준점 gunPivot을 3D 모델의 오른쪽 팔꿈치 위치로 이동
        gunPivot.position = playerAnimator.GetIKHintPosition(AvatarIKHint.RightElbow);

        // IK를 사용하여 왼손의 위치와 회전을 총의 오른쪽 손잡이에 맞춘다
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandMount.rotation);

        // IK를 사용하여 오른손의 위치와 회전을 총의 오른쪽 손잡이에 맞춘다
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1.0f);

        playerAnimator.SetIKPosition(AvatarIKGoal.RightHand, rightHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.RightHand, rightHandMount.rotation);
    }
}