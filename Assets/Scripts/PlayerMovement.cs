using Photon.Pun;
using UnityEngine;

// 플레이어 캐릭터를 사용자 입력에 따라 움직이는 스크립트
public class PlayerMovement : MonoBehaviourPun {
    public float moveSpeed = 5f; // 앞뒤 움직임의 속도
    public float rotateSpeed = 180f; // 좌우 회전 속도

    private Animator playerAnimator; // 플레이어 캐릭터의 애니메이터
    private PlayerInput playerInput; // 플레이어 입력을 알려주는 컴포넌트
    private Rigidbody playerRigidbody; // 플레이어 캐릭터의 리지드바디

    private void Start() {
        // 사용할 컴포넌트들의 참조를 가져오기
        playerInput = GetComponent<PlayerInput>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerAnimator = GetComponent<Animator>();
    }

    // FixedUpdate는 물리 갱신 주기에 맞춰 실행됨
    private void FixedUpdate() {
        // 로컬 플레이어만 직접 위치와 회전을 변경 가능
        if (!photonView.IsMine)
        {
            return;
        }

        // 회전 실행
        Rotate();
        // 움직임 실행
        Move();

        // 입력값에 따라 애니메이터의 Move 파라미터 값을 변경
        playerAnimator.SetFloat("Move", playerInput.move);
    }

    // 입력값에 따라 캐릭터를 앞뒤로 움직임
    private void Move() {
        // 상대적으로 이동할 거리 계산
        Vector3 moveDistance =
            playerInput.move * transform.forward * moveSpeed * Time.deltaTime;
        // 리지드바디를 통해 게임 오브젝트 위치 변경
        playerRigidbody.MovePosition(playerRigidbody.position + moveDistance);
    }

    // 입력값에 따라 캐릭터를 좌우로 회전
    private void Rotate() {
        Vector3 mouseScreenPosition = Input.mousePosition;

        // 메인 카메라를 기준으로 마우스 위치를 월드 좌표로 변환
        Ray ray = Camera.main.ScreenPointToRay(mouseScreenPosition);
        Plane plane = new Plane(Vector3.up, transform.position);

        if (plane.Raycast(ray, out float distance))
        {
            // 월드 좌표로 변환된 마우스 위치를 얻음
            Vector3 targetPoint = ray.GetPoint(distance);

            // 타겟 방향 계산
            Vector3 direction = (targetPoint - transform.position).normalized;
            direction.y = 0; // y축 방향 제거 (평면상에서만 회전하도록)

            // 회전할 각도 계산
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // 리지드바디를 사용하여 회전
            playerRigidbody.rotation = Quaternion.Slerp(playerRigidbody.rotation, targetRotation, rotateSpeed * Time.deltaTime);
        }
    }
}