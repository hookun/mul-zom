using System.Collections;
using System.Reflection;
using Photon.Pun;
using UnityEngine;

// 총을 구현한다
public class Gun : MonoBehaviourPun, IPunObservable {
    // 총의 상태를 표현하는데 사용할 타입을 선언한다
    public enum State {
        Ready, // 발사 준비됨
        Empty, // 탄창이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 총알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    public LineRenderer[] bulletLineRenderer; // 총알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기
    
    public GunData gunData; // 총의 현재 데이터
    public string whoshoot;
    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄약
    public int magAmmo; // 현재 탄창에 남아있는 탄약

    private float lastFireTime; // 총을 마지막으로 발사한 시점

    
    public int magazinSize;

    public bool isSingleOnly = false;
    public bool isShotgun = false;
    public int gunID;
    public float reloadTime;
   



    // 주기적으로 자동 실행되는, 동기화 메서드
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info) {
        // 로컬 오브젝트라면 쓰기 부분이 실행됨
        if (stream.IsWriting)
        {
            // 남은 탄약수를 네트워크를 통해 보내기
            stream.SendNext(ammoRemain);
            // 탄창의 탄약수를 네트워크를 통해 보내기
            stream.SendNext(magAmmo);
            // 현재 총의 상태를 네트워크를 통해 보내기
            stream.SendNext(state);
        }
        else
        {
            // 리모트 오브젝트라면 읽기 부분이 실행됨
            // 남은 탄약수를 네트워크를 통해 받기
            ammoRemain = (int) stream.ReceiveNext();
            // 탄창의 탄약수를 네트워크를 통해 받기
            magAmmo = (int) stream.ReceiveNext();
            // 현재 총의 상태를 네트워크를 통해 받기
            state = (State) stream.ReceiveNext();
        }
    }

    // 남은 탄약을 추가하는 메서드
    [PunRPC]
    public void AddAmmo(int ammo) {
        ammoRemain += ammo;
    }

    private void Awake() {
        // 사용할 컴포넌트들의 참조를 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        reloadTime = gunData.reloadTime;
        if (isShotgun)
        {
            gunID = 3;
            fireDistance = 5f;
            for (int i = 0; i < bulletLineRenderer.Length; i++)
            {              
                bulletLineRenderer[i].enabled = false; // 초기에는 비활성화 상태
            }
        }
        else if (!isShotgun)
        {
            bulletLineRenderer = new LineRenderer[1];
            bulletLineRenderer[0] = GetComponent<LineRenderer>();
            bulletLineRenderer[0].positionCount = 2;
            bulletLineRenderer[0].enabled = false;
            if (isSingleOnly)
                gunID = 1;
            else gunID = 0;
        }
    }


    private void OnEnable() {
        // 전체 예비 탄약 양을 초기화
        ammoRemain = gunData.startAmmoRemain;
        // 현재 탄창을 가득채우기
        magAmmo = gunData.magCapacity;

        // 총의 현재 상태를 총을 쏠 준비가 된 상태로 변경
        state = State.Ready;
        // 마지막으로 총을 쏜 시점을 초기화
        lastFireTime = 0;
        
    }


    // 발사 시도
    public void Fire() {
        // 현재 상태가 발사 가능한 상태
        // && 마지막 총 발사 시점에서 timeBetFire 이상의 시간이 지남
        if (state == State.Ready
            && Time.time >= lastFireTime + gunData.timeBetFire)
        {
            // 마지막 총 발사 시점을 갱신
            lastFireTime = Time.time;
            // 실제 발사 처리 실행
            Shot();
        }
    }

    private void Shot() {
        // 실제 발사 처리는 호스트에게 대리
        photonView.RPC("ShotProcessOnServer", RpcTarget.MasterClient);

        // 남은 탄환의 수를 -1
        magAmmo--;
        if (magAmmo <= 0)
        {
            // 탄창에 남은 탄약이 없다면, 총의 현재 상태를 Empty으로 갱신
            state = State.Empty;
        }
    }
    public void Single_Fire()//단발 발사
    {
        //현재 발사가 가능한 상태 && 총 발사 간격의 시간이 지났는지
        if (state == State.Ready && Time.time >= lastFireTime +
            gunData.timeBetFire)
        {
            lastFireTime = Time.time;
            Shot();
        }
    }
    // 호스트에서 실행되는, 실제 발사 처리
    [PunRPC]
    private void ShotProcessOnServer() {
        int rayCount = bulletLineRenderer.Length; // 발사할 레이의 수
        float angleSpread = 10f; // 레이의 각도 간격

        for (int i = 0; i < rayCount; i++)
        {
            float angle = -angleSpread * (rayCount - 1) / 2 + angleSpread * i;
            Vector3 rayDirection = Quaternion.Euler(0, angle, 0) * fireTransform.forward;
            // 레이캐스트에 의한 충돌 정보를 저장하는 컨테이너
            RaycastHit hit;
            // 총알이 맞은 곳을 저장할 변수
            Vector3 hitPosition = Vector3.zero;

            // 레이캐스트(시작지점, 방향, 충돌 정보 컨테이너, 사정거리)
            if (Physics.Raycast(fireTransform.position,
                rayDirection, out hit, fireDistance))
            {
                // 레이가 어떤 물체와 충돌한 경우

                // 충돌한 상대방으로부터 IDamageable 오브젝트를 가져오기 시도
                IDamageable target =
                    hit.collider.GetComponent<IDamageable>();

                // 상대방으로 부터 IDamageable 오브젝트를 가져오는데 성공했다면
                if (target != null)
                {
                    PhotonView parentPhotonView = transform.root.GetComponent<PhotonView>();
                    // 상대방의 OnDamage 함수를 실행시켜서 상대방에게 데미지 주기
                    target.OnDamage(gunData.damage, hit.point, hit.normal, parentPhotonView.ViewID);
                    print("aa" + photonView.Owner.NickName);//맞은 대상확인
                }

                // 레이가 충돌한 위치 저장
                hitPosition = hit.point;
            }
            else
            {
                // 레이가 다른 물체와 충돌하지 않았다면
                // 총알이 최대 사정거리까지 날아갔을때의 위치를 충돌 위치로 사용
                hitPosition = fireTransform.position +
                              rayDirection * fireDistance;
            }

            // 발사 이펙트 재생, 이펙트 재생은 모든 클라이언트들에서 실행
            photonView.RPC("ShotEffectProcessOnClients", RpcTarget.All, hitPosition, photonView.ViewID,i);
        }
    }

    // 이펙트 재생 코루틴을 랩핑하는 메서드
    [PunRPC]
    private void ShotEffectProcessOnClients(Vector3 hitPosition, int shooterID,int index) {
        PhotonView shooterPhotonView = PhotonView.Find(shooterID); 
        if (shooterPhotonView != null) { 
            Debug.Log("Shot fired by: " + shooterPhotonView.Owner.NickName); //발사한 사람 확인
        }
        StartCoroutine(ShotEffect(hitPosition, fireTransform.position, index));
    }

    // 발사 이펙트와 소리를 재생하고 총알 궤적을 그린다
    private IEnumerator ShotEffect(Vector3 hitPosition, Vector3 firePosition, int index) {
        // 총구 화염 효과 재생
        muzzleFlashEffect.Play();
        // 탄피 배출 효과 재생
        shellEjectEffect.Play();

        // 총격 소리 재생
        gunAudioPlayer.PlayOneShot(gunData.shotClip);
        LineRenderer lineRenderer = bulletLineRenderer[index];

        // 라인 렌더러의 시작점과 끝점 설정
        lineRenderer.SetPosition(0, firePosition);
        lineRenderer.SetPosition(1, hitPosition);
        lineRenderer.enabled = true;

        

        // 0.03초 동안 잠시 처리를 대기
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 총알 궤적을 지운다
        lineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload() {
        if (state == State.Reloading ||
            ammoRemain <= 0 || magAmmo >= gunData.magCapacity)
        {
            // 이미 재장전 중이거나, 남은 총알이 없거나
            // 탄창에 총알이 이미 가득한 경우 재장전 할수 없다
            return false;
        }

        // 재장전 처리 실행
        StartCoroutine(ReloadRoutine());
        return true;
    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine() {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;
        // 재장전 소리 재생
        gunAudioPlayer.PlayOneShot(gunData.reloadClip);

        // 재장전 소요 시간 만큼 처리를 쉬기
        yield return new WaitForSeconds(gunData.reloadTime);

        // 탄창에 채울 탄약을 계산한다
        int ammoToFill = gunData.magCapacity - magAmmo;

        // 탄창에 채워야할 탄약이 남은 탄약보다 많다면,
        // 채워야할 탄약 수를 남은 탄약 수에 맞춰 줄인다
        if (ammoRemain < ammoToFill)
        {
            ammoToFill = ammoRemain;
        }

        // 탄창을 채운다
        magAmmo += ammoToFill;
        // 남은 탄약에서, 탄창에 채운만큼 탄약을 뺸다
        ammoRemain -= ammoToFill;

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}