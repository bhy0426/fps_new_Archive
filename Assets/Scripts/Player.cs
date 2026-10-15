using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // --오브젝트
    // 카메라
    public Camera mainCam; // 메인 카메라(플레이어 눈)
    public Camera weaponCam; // 무기 카메라

    // 총기
    public Weapon equipWeapon; // 현재 사용 중인 무기 정보(type, damage, ..)
    public GameObject[] weapons; // 플레이어 무기 오브젝트 배열
    public Transform gunGroup; // 총기 위치
    public Transform adsPivot;

    // 플레이어 몸
    public GameObject playerBody;
    public GameObject playerArm;
    public GameObject playerWeaponAndHand;

    // 플레이어 총 & 손 애니메이션
    public Animator animCon;
    public AnimationClip[] playerAnims;


    // --변수
    // 체력
    public int hp;
    public int maxHp;

    // 이동 관련
    float moveX; // 좌우 이동
    float moveZ; // 전후 이동
    Vector3 moveVec;

    public float speed; // 현재 이동 속도
    public float walkSpeed; // 걷기 속도
    public float sprintSpeed; // 달리기 속도
    public float crouchSpeed; // 앉기 속도
    public float proneSpeed; // 엎드리기 속도
    public float jumpForce; // 점프
    public float gravityForce; // 중력

    // 카메라 관련
    float mouseX = 0f; // 마우스 좌우 이동 (카메라 상하 이동)
    float mouseY = 0f; // 마우스 상하 이동 (카메라 좌우 이동)
    public float sens; // 회전 감도
    public float[] postureFOV; // 자세 FOV

    // 상태 확인
    public bool isFire = false; // 발사 쿨다운
    public bool isReload = false; // 재장전 확인
    [SerializeField]
    bool isGround = false; // 지면에 닿았는지 확인
    [SerializeField]
    bool isProneOriginMove = false;
    bool isJump = false;

    // 총기
    [SerializeField]
    float accuracy = 0.01f; // 현재 정확도
    float curAtkRate; // 다음 발사 까지의 시간
    // float ggY = -0.403f;
    // float ggZ = 0.4f;
    public float recoilSum = 0f;
    public float recoil = 0f;
    // 테스트
    public AudioSource audioSource;


    // 플레이어 자세
    enum PlayerPosture
    {
        idle,
        walk,
        sprint,
        crouch,
        prone
    }

    // 플레이어 콜라이더
    enum PlayerCollider
    {
        originCollider,
        crouchCollider,
        proneCollider
    }

    // 플레이어 조준 상태
    enum PlayerAimState
    {
        hipFire,
        ads
    }

    // 플레이어 컴포넌트
    Rigidbody rigid; // 플레이어 Rigidbody
    CapsuleCollider[] capsuleColliders; // 플레이어 콜라이더 컴포넌트 배열
    

    [SerializeField]
    PlayerCollider playerCollider; // 현재 플레이어의 콜라이더
    [SerializeField]
    PlayerPosture playerPosture; // 현재 플레이어의 자세 상태
    [SerializeField]
    PlayerAimState playerAimState; // 현재 플레이어의 조준 상태

    [Header("Weapon Sway")]
    public float swayAmount;   // 흔들리는 정도
    public float maxSwayAmount; // 최대 흔들림 제한
    public float swaySmoothness;   // 부드러운 정도 (높을수록 빠름)
    public GameObject swayPivot;

    // 테스트용 재장전 코루틴
    private Coroutine reloadCoroutine;

    // 씬 처음 시작시 호출
    void Awake()
    {
        // 플레이어 컴포넌트 변수 초기화
        rigid = GetComponent<Rigidbody>();
        capsuleColliders = GetComponents<CapsuleCollider>();
    }

    private void Start()
    {
        mouseY = transform.localEulerAngles.x;
        mouseX = transform.localEulerAngles.y;
    }

    // 현재 게임 프레임마다 호출
    void Update()
    {
        // 카메라
        CameraRotation(); // 카메라 회전
        WeaponSway();

        // 공격
        Attack(); // 발사
        Reload(); // 재장전
        Swap(); // 총기 교체
        ADS(); // 정조준

        // 자세 변경
        Sprint(); // 달리기
        Crouch(); // 앉기
        Prone(); // 엎드리기

        // 점프
        IsJump();

        // 추후에 UI는 따로 빼기
        // 자세에 따른 UI Text 변경
        GameManager.instance.playerStateText.text = playerPosture.ToString();

        // 탄약 UI
        GameManager.instance.ammoText.text = equipWeapon.curAmmo + " / " + equipWeapon.totalAmmo;

        // 메인 캠 방향 Ray
        // Debug.DrawRay(mainCam.transform.position, mainCam.transform.forward * 100f, Color.green);
    }

    // 고정된 프레임마다 호출(fixedDeltaTime)
    private void FixedUpdate()
    {
        // 상태에 따른 속도 변경
        UpdateMovement(); // 자세에 따른 속도 변경

        // 이동
        Move(); // WASD 이동
        Jump(); // 점프
        Gravity(); // 중력 구현
    }

    private void WeaponSway()
    {
        // 마우스 입력값 받기
        float moveX = -Input.GetAxis("Mouse X") * swayAmount;
        float moveY = -Input.GetAxis("Mouse Y") * swayAmount;
        
        // 최대 흔들림 범위 제한
        moveX = Mathf.Clamp(moveX, -maxSwayAmount, maxSwayAmount);
        moveY = Mathf.Clamp(moveY, -maxSwayAmount, maxSwayAmount);

        // 목표 위치 계산
        Vector3 targetPos = new Vector3(moveX, moveY, 0);

        // 현재 총기 위치에서 목표 위치로 부드럽게 이동 (Lerp)
        // playerWeaponAndHand 대신 weaponBodyGroup(이전에 만든 부모 오브젝트)에 적용하는 것을 추천합니다.
        swayPivot.transform.localPosition = Vector3.Lerp(
            swayPivot.transform.localPosition,
            targetPos,
            Time.deltaTime * swaySmoothness
        );
    }

    // 카메라 움직임
    private void CameraRotation()
    {
        // 마우스 움직임
        mouseX += Input.GetAxis("Mouse X") * sens; //* Time.deltaTime;
        mouseY += Input.GetAxis("Mouse Y") * sens; //* Time.deltaTime;

        mouseY = Mathf.Clamp(mouseY, -90, 90); // 카메라의 X축 회전을 -90 ~ 90으로 제한

        // 카메라의 X축은 위 -, 아래 + 이므로 X축에 -mouseY를 대입
        // 카메라의 Y축은 오른쪽 +, 왼쪽 - 이므로 Y축에 mouseX를 대입
        transform.localRotation = Quaternion.Euler(0, mouseX, 0); // 플레이어는 Y축(좌우)만 회전
        mainCam.transform.localRotation = Quaternion.Euler(-mouseY, 0, 0); // 카메라는 X축(상하)만 회전

        // 자세에 따른 카메라 위치 변경
        // Lerp 메소드를 이용하여 선형 이동
        mainCam.transform.position = new Vector3(mainCam.transform.position.x,
            Mathf.Lerp(mainCam.transform.position.y, (transform.position + capsuleColliders[(int)playerCollider].center + (transform.up * 0.75f)).y, Time.deltaTime * 10f),
            mainCam.transform.position.z);
    }

    // 공격
    private void Attack()
    {
        // 기존 총기 위치
        Vector3 originVec = new Vector3(0.2f, -0.403f, 0.4f);

        // 조준점 위치
        float originDotPos = GameManager.instance.originDotPos;
        float recoilDotPos = GameManager.instance.recoilDotPos;


        // 테스트
        // Vector3 recoilGun = new Vector3(originVec.x, Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        // gunGroup.transform.localPosition = new Vector3(originVec.x, ggY, ggZ);


        // 마우스 왼쪽 버튼 && 사격 중이 아닐 때 && 재장전 중이 아닐 때
        if (Input.GetButton("Fire1") && !isFire && !isReload)
        {
            // 달리기 자세가 아닐 시
            if (playerPosture != PlayerPosture.sprint)
            {
                // 현재 탄약이 0보단 큰 경우에만 사격 가능
                if (equipWeapon.curAmmo > 0)
                {
                    // 사격 코루틴 호출
                    StopCoroutine(equipWeapon.Fire(mainCam.transform));
                    StartCoroutine(equipWeapon.Fire(mainCam.transform));
                    audioSource.PlayOneShot(audioSource.clip);
                    // 다음 발사 까지의 시간 초기화
                    curAtkRate = 0;

                    // 현재 무기의 현재 탄약 감소
                    equipWeapon.curAmmo--;

                    // 테스트
                    // Debug.Log("Test");
                    // ggY += Random.Range(-1f, 1f) * Time.deltaTime;
                    // ggZ += Random.Range(-1f, 1f) * Time.deltaTime;
                    // accuracy *= 1.2f;
                    // 
                    // mouseY += accuracy;
                    // mouseX += Random.Range(-0.1f, 0.1f);

                    // gunGroup.transform.localPosition = Vector3.Lerp(originVec, recoilGun, Time.deltaTime);

                    // if (accuracy <= 0.1f)
                    // {
                    //     
                    // 
                    //     // accuracy += accuracy * 5f * Time.deltaTime;
                    //     
                    // 
                    //     // 카메라 반동
                    //     
                    // }
                }
                // 만약 0인 경우 사격하면 자동으로 재장전
                // 추후에 기능을 뺄 수도 있음
                else
                {
                    // 재장전 코루틴 호출
                    StopCoroutine(equipWeapon.ReloadCoroutine());
                    StartCoroutine(equipWeapon.ReloadCoroutine());

                    if (playerAimState == PlayerAimState.ads)
                    {
                        // 지향사격 활성화
                        playerAimState = PlayerAimState.hipFire;

                        // 지향사격 조준점 활성화
                        GameManager.instance.originDot.SetActive(true);

                        // 정조준 조준점 비활성화
                        GameManager.instance.adsDot.SetActive(false);
                    }
                }
            }
            // 달리기 자세일 시
            else
            {
                playerPosture = PlayerPosture.idle;
            }
        }


        // 총기 발사 속도 타이머
        curAtkRate += Time.deltaTime;

        // 사격 중일 때 반동 효과
        if (isFire)
        {
            // 타이머가 총기 발사 속도의 절반 미만이면
            if (curAtkRate < equipWeapon.attackRate / 2)
            {
                // 반동 벡터
                // Vector3 recoilVec = new Vector3(accuracy, Random.Range(-1f, 1f), 0f);
                // Vector3 recoilGun = new Vector3(originVec.x, Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                // Vector3 recoilGun = new Vector3(0f, 0f, Random.Range(-4f, 4f));
                Vector3 recoilGunX = new Vector3(Random.Range(0f, 1f), 0f, 0f);
                Vector3 recoilGunZ = new Vector3(0f, 0f, Random.Range(-0.025f, 0f));


                // 카메라 반동
                if (accuracy <= 1f)
                {
                    accuracy += accuracy * 5f * Time.deltaTime;

                    // 카메라 반동
                    mouseY += accuracy;

                    // 카메라 반동 합
                    // recoilSum += accuracy;
                }

                mouseX += Random.Range(-0.1f, 0.1f);
                mouseY += Random.Range(-0.01f, 0.05f);

                // 집탄율
                if (equipWeapon.recoil < 0.08f)
                {
                    if (playerAimState == PlayerAimState.hipFire)
                    {
                        equipWeapon.recoil *= 1.01f;
                    }
                }

                // 카메라 반동
                // mainCam.transform.localEulerAngles += Vector3.Lerp(Vector3.zero, -recoilVec, Time.deltaTime * 10f);

                // 총기 반동
                switch (playerAimState)
                {
                    case PlayerAimState.hipFire:
                        // X축 회전으로 구현
                        playerWeaponAndHand.transform.localEulerAngles = recoilGunX;
                        // playerWeaponAndHand.transform.localEulerAngles = Vector3.Lerp(playerWeaponAndHand.transform.localEulerAngles, playerWeaponAndHand.transform.localEulerAngles - (recoilGunX * 2f), Time.deltaTime * 10f);
                        playerWeaponAndHand.transform.localPosition = recoilGunZ;
                        break;

                    case PlayerAimState.ads:
                        // Z축 이동으로 구현
                        playerWeaponAndHand.transform.localPosition = recoilGunZ;
                        break;
                }

                // Z축 이동으로 구현
                // playerWeaponAndHand.transform.localEulerAngles = Vector3.Lerp(Vector3.zero, recoilGun, Time.deltaTime);

                // X축 회전으로 구현
                // playerWeaponAndHand.transform.localEulerAngles = recoilGun;


                // animCon.SetBool("isShoot", true);


                // 조준점 간격 증가
                GameManager.instance.dotTopRect.localPosition = Vector3.Lerp(GameManager.instance.dotTopRect.localPosition, new Vector3(0f, recoilDotPos, 0f), Time.deltaTime * 10f);
                GameManager.instance.dotBottomRect.localPosition = Vector3.Lerp(GameManager.instance.dotBottomRect.localPosition, new Vector3(0f, -recoilDotPos, 0f), Time.deltaTime * 10f);
                GameManager.instance.dotRightRect.localPosition = Vector3.Lerp(GameManager.instance.dotRightRect.localPosition, new Vector3(recoilDotPos, 0, 0f), Time.deltaTime * 10f);
                GameManager.instance.dotLeftRect.localPosition = Vector3.Lerp(GameManager.instance.dotLeftRect.localPosition, new Vector3(-recoilDotPos, 0f, 0f), Time.deltaTime * 10f);
            }
            // 타이머가 총기 발사 속도의 절반 이상이면
            else
            {
                // 조준점 간격 감소
                GameManager.instance.dotTopRect.localPosition = Vector3.Lerp(GameManager.instance.dotTopRect.localPosition, new Vector3(0f, originDotPos, 0f), Time.deltaTime * 10f);
                GameManager.instance.dotBottomRect.localPosition = Vector3.Lerp(GameManager.instance.dotBottomRect.localPosition, new Vector3(0f, -originDotPos, 0f), Time.deltaTime * 10f);
                GameManager.instance.dotRightRect.localPosition = Vector3.Lerp(GameManager.instance.dotRightRect.localPosition, new Vector3(originDotPos, 0f, 0f), Time.deltaTime * 10f);
                GameManager.instance.dotLeftRect.localPosition = Vector3.Lerp(GameManager.instance.dotLeftRect.localPosition, new Vector3(-originDotPos, 0f, 0f), Time.deltaTime * 10f);

                // recoil = mouseY - recoilSum;
            }
        }
        // 사격 중지시
        else
        {
            animCon.SetBool("isShoot", false);

            // 카메라 반동 초기화
            accuracy = 0.1f;

            // 총기 반동 총기화
            if (playerAimState == PlayerAimState.hipFire)
            {
                // 지향 사격 총기 반동 기본값
                equipWeapon.recoil = 0.04f;
            }
            else
            {
                // 정조준 사격 총기 반동 기본값
                equipWeapon.recoil = 0.00f;
            }

            // 카메라 반동 회복
            // 마우스 위치에서 반동 값만큼을 서서히 감소시킴
            // 반동 값만큼 감소됐으면 반복 종료
            if(recoilSum > 0)
            {
                recoilSum = Mathf.Lerp(recoilSum, 0, Time.deltaTime * 10f);
            }

            // 총기 반동
            switch (playerAimState)
            {
                case PlayerAimState.hipFire:
                    // X축 회전으로 구현
                    playerWeaponAndHand.transform.localEulerAngles = Vector3.zero;
                    break;

                case PlayerAimState.ads:
                    // Z축 이동으로 구현
                    playerWeaponAndHand.transform.localPosition = Vector3.zero;
                    break;
            }

            // 총기 반동 회복(위치 초기화)
            // playerWeaponAndHand.transform.localEulerAngles = Vector3.zero;

            // playerBody.transform.localPosition = new Vector3(0f, -2.811f, 0f); // 플레이어 몸 위치

            // 조준점 원위치
            GameManager.instance.dotTopRect.localPosition = Vector3.Lerp(GameManager.instance.dotTopRect.localPosition, new Vector3(0f, originDotPos, 0f), Time.deltaTime * 10f);
            GameManager.instance.dotBottomRect.localPosition = Vector3.Lerp(GameManager.instance.dotBottomRect.localPosition, new Vector3(0f, -originDotPos, 0f), Time.deltaTime * 10f);
            GameManager.instance.dotRightRect.localPosition = Vector3.Lerp(GameManager.instance.dotRightRect.localPosition, new Vector3(originDotPos, 0f, 0f), Time.deltaTime * 10f);
            GameManager.instance.dotLeftRect.localPosition = Vector3.Lerp(GameManager.instance.dotLeftRect.localPosition, new Vector3(-originDotPos, 0f, 0f), Time.deltaTime * 10f);
        }
    }

    // 재장전
    private void Reload()
    {
        // R키 && 재장전 중이 아닐 때
        if(Input.GetButtonDown("Reload") && !isReload)
        {
            if(equipWeapon.curAmmo < equipWeapon.maxAmmo)
            {
                // 재장전 코루틴 호출
                // StopCoroutine(equipWeapon.ReloadCoroutine());
                reloadCoroutine = StartCoroutine(equipWeapon.ReloadCoroutine());

                // 달리기 자세일 시 기본 자세로 변경 
                if(playerPosture == PlayerPosture.sprint)
                {
                    // 기본 자세로 변경
                    playerPosture = PlayerPosture.idle;
                }

                // 지향사격 활성화
                playerAimState = PlayerAimState.hipFire;

                // 지향사격 조준점 활성화
                GameManager.instance.originDot.SetActive(true);

                // 정조준 조준점 비활성화
                GameManager.instance.adsDot.SetActive(false);
            }
        }
    }

    // 총기 교체
    private void Swap()
    {
        // 재장전 중이 아닐 때
        if(!isReload)
        {
            // 현재 장착 중인 무기가 없으면 리턴
            if (equipWeapon.gameObject == null)
                return;

            // 배열 길이가 2이기 때문에, 0과 1만 반복
            for (int i = 0; i < weapons.Length; i++)
            {
                // 1, 2번 키를 누르면 무기 교체
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    ChangeWeapon(i);
                }
            }
        }
    }
    private void ChangeWeapon(int index)
    {
        weapons[(int)equipWeapon.type].SetActive(false); // 현재 사용 중인 무기 비활성화
        equipWeapon = weapons[index].GetComponent<Weapon>(); // equipWeapon 변경
        weapons[(int)equipWeapon.type].SetActive(true); // 현재 사용될 무기 활성화
        equipWeapon.player = this; // player 스크립트 전달
    }

    // 정조준
    private void ADS()
    {
        // 마우스 오른쪽 버튼
        if (Input.GetButtonDown("ADS") && !isReload) // && playerPosture != PlayerPosture.prone)
        {
            // 정조준 상태이면
            if(playerAimState == PlayerAimState.ads)
            {
                // 지향사격 활성화
                playerAimState = PlayerAimState.hipFire;

                // 지향사격 조준점 활성화
                GameManager.instance.originDot.SetActive(true);

                // 정조준 조준점 비활성화
                // GameManager.instance.adsDot.SetActive(false);
            }
            else
            {
                // 정조준 활성화
                playerAimState = PlayerAimState.ads;

                // 지향사격 조준점 활성화
                GameManager.instance.originDot.SetActive(false);

                // 정조준 조준점 비활성화
                // GameManager.instance.adsDot.SetActive(true);

                // 달리는 중에 정조준을 하면 걷기로 변경
                if(playerPosture == PlayerPosture.sprint)
                    playerPosture = PlayerPosture.walk;
            }
        }

        // 정조준 상태이면
        if (playerAimState == PlayerAimState.ads)
        {
            // 무기 위치를 정조준 위치로 변경
            //equipWeapon.transform.localPosition = Vector3.Lerp(equipWeapon.transform.localPosition, equipWeapon.adsPos, Time.deltaTime * 30f);
            //playerBody.transform.localPosition = Vector3.Lerp(playerBody.transform.localPosition, new Vector3(-0.2f, -2.811f, 0f), Time.deltaTime * 30f);
            adsPivot.localPosition = Vector3.Lerp(adsPivot.localPosition, new Vector3(-0.2f, 0.11f, 0f), Time.deltaTime * 15f);
            // 플레이어 몸 오브젝트 비활성화
            // playerArm.SetActive(false);
        }
        // 지향사격 상태이면
        else
        {
            // 무기 위치를 지향사격 위치로 변경
            //equipWeapon.transform.localPosition = Vector3.Lerp(equipWeapon.transform.localPosition, equipWeapon.originPos, Time.deltaTime * 30f);
            //playerBody.transform.localPosition = Vector3.Lerp(playerBody.transform.localPosition, new Vector3(0f, -2.811f, 0f), Time.deltaTime * 30f);
            adsPivot.localPosition = Vector3.Lerp(adsPivot.localPosition, Vector3.zero, Time.deltaTime * 15f);
            // 플레이어 몸 오브젝트 활성화
            // playerArm.SetActive(true);
        }
    }

    // 달리기
    private void Sprint()
    {
        // Shift 키
        // 지면에 닿았고 W키를 눌렀을 때만 달리기 가능(옆으로 달리기 제한)
        if (Input.GetButtonDown("Sprint"))
        {
            // 만약 달리기 자세가 아니면 달리기 자세로 변경
            if (playerPosture != PlayerPosture.sprint && isGround)
            {
                // 재장전 중이면 재장전 코루틴 중단
                if(isReload)
                {
                    // 재장전 애니메이션 중단
                    animCon.SetBool("isReload", false);
                    StopCoroutine(reloadCoroutine);
                    animCon.CrossFadeInFixedTime("Player Sprint", 0.1f);
                    isReload = false;
                    Debug.Log("코루틴 중단");
                }


                // 현재 플레이어의 콜라이더 상태 변경
                playerCollider = PlayerCollider.originCollider;

                // 현재 플레이어의 자세 변경
                playerPosture = PlayerPosture.sprint;

                // 현재 플레이어 콜라이더 컴포넌트 변경
                ChangeCollider(playerCollider);

                // 현재 정조준 중이면 정조준 해제
                playerAimState = PlayerAimState.hipFire;
                GameManager.instance.originDot.SetActive(true);
                GameManager.instance.adsDot.SetActive(false);

                // 달리기 애니메이션
                animCon.SetBool("isSprint", true);
            }
            // 만약 달리기 자세에서 SHIFT키를 한번 더 누르면 기본 자세로 변경
            else if (playerPosture == PlayerPosture.sprint)
            {
                // 현재 플레이어의 콜라이더 상태 변경
                playerCollider = PlayerCollider.originCollider;

                // 현재 플레이어의 자세 변경
                playerPosture = PlayerPosture.idle;

                // 현재 플레이어 콜라이더 컴포넌트 변경
                ChangeCollider(playerCollider);

                // 달리기 애니메이션
                animCon.SetBool("isSprint", false);
            }
        }

        // 달리다가 W키를 떼면 걷기로 전환
        // 현재 이 코드로 인해 자세가 바뀌지 않음

        // 달리다가 이동을 멈추면 기본 자세로 변경
        if (moveZ <= 0 && playerPosture == PlayerPosture.sprint)
        {
            // 현재 플레이어의 콜라이더 상태 변경
            playerCollider = PlayerCollider.originCollider;
        
            // 현재 플레이어의 자세 변경
            playerPosture = PlayerPosture.idle;
        
            // 현재 플레이어 콜라이더 컴포넌트 변경
            ChangeCollider(playerCollider);
        
            // 달리기 애니메이션
            animCon.SetBool("isSprint", false);
        }

        // 달리고 있을 때 카메라 반동 효과
        if(playerPosture == PlayerPosture.walk && moveVec != Vector3.zero)
        {
            // 카메라 반동 Head Bop
            float bop;

            // 어떤 값이 들어가도 -1 ~ 0 ~ 1을 반복
            // Mathf.Sion(Time.time * 속도) * 크기
            bop = Mathf.Sin(Time.time * 15f) * 0.05f;
            mainCam.transform.localPosition = Vector3.Lerp(mainCam.transform.localPosition, new Vector3(0f, bop, 0f), Time.deltaTime * 10f);
        }
        else if(playerPosture == PlayerPosture.sprint)
        {
            // 카메라 반동 Head Bop
            float bop;

            // 어떤 값이 들어가도 -1 ~ 0 ~ 1을 반복
            // Mathf.Sion(Time.time * 속도) * 크기
            bop = Mathf.Sin(Time.time * 25f) * 0.1f;
            mainCam.transform.localPosition = Vector3.Lerp(mainCam.transform.localPosition, new Vector3(0f, bop, 0f), Time.deltaTime * 10f);
        }
    }

    // 앉기 
    private void Crouch()
    {
        // 지면에 닿았을 때만 앉기 가능
        if(Input.GetButtonDown("Crouch") && isGround)
        {
            // 만약 앉기 자세가 아니면 앉기 자세로 변경
            if (playerPosture != PlayerPosture.crouch)
            {
                // 현재 플레이어의 콜라이더 상태 변경
                playerCollider = PlayerCollider.crouchCollider;

                // 현재 플레이어의 자세 변경
                playerPosture = PlayerPosture.crouch;

                // 현재 플레이어 콜라이더 컴포넌트 변경
                ChangeCollider(playerCollider);
            }

            // 만약 앉기 자세에서 CTRL키를 한번 더 누르면 기본 자세로 변경
            else if (playerPosture == PlayerPosture.crouch)
            {
                // 현재 플레이어의 콜라이더 상태 변경
                playerCollider = PlayerCollider.originCollider;

                // 현재 플레이어의 자세 변경
                playerPosture = PlayerPosture.walk;

                // 현재 플레이어 콜라이더 컴포넌트 변경
                ChangeCollider(playerCollider);
            }
        }
    }

    // 엎드리기
    private void Prone()
    {
        // 지면에 닿았을 때만 엎드리기 가능
        if(Input.GetButtonDown("Prone") && isGround)
        {
            // 만약 엎드리기 자세가 아니면 엎드리기 자세로 변경
            if (playerPosture != PlayerPosture.prone)
            {
                // 현재 플레이어의 콜라이더 변경
                playerCollider = PlayerCollider.proneCollider;

                // 현재 플레이어의 자세 변경
                playerPosture = PlayerPosture.prone;

                // 현재 플레이어 콜라이더 컴포넌트 변경
                ChangeCollider(playerCollider);
            }

            // 만약 엎드리기 자세에서 Z키를 한번 더 누르면 기본 자세로 변경
            else if (playerPosture == PlayerPosture.prone)
            {
                // 현재 플레이어의 콜라이더 변경
                playerCollider = PlayerCollider.originCollider;

                // 현재 플레이어의 자세 변경
                playerPosture = PlayerPosture.walk;

                // 현재 플레이어 콜라이더 컴포넌트 변경
                ChangeCollider(playerCollider);
            }
        }

        // 엎드리다가 W키를 누르면 정조준 해제
        // W키 && 엎드리기 자세
    }

    // 콜라이더 컴포넌트 변경
    private void ChangeCollider(PlayerCollider playerColliderPara)
    {
        // 콜라이더 컴포넌트 배열의 크기만큼 반복
        for(int i = 0; i < capsuleColliders.Length; i++)
        {
            // 인자로 받은 자세 콜라이더를 활성화
            if(i == (int)playerColliderPara)
            {
                capsuleColliders[i].enabled = true;
            }
            // 인자로 받지 않은 콜라이더는 비활성화
            else
            {
                capsuleColliders[i].enabled = false;
            }
        }

        // 현재 플레이어의 콜라이더 변경
        playerCollider = playerColliderPara;
    }

    // 플레이어의 자세에 따른 속도 조절
    private void UpdateMovement()
    {
        switch (playerPosture)
        {
            case PlayerPosture.idle:

                // 속도 변경
                speed = 0;

                // 지향사격 상태
                if (playerAimState == PlayerAimState.hipFire)
                {
                    // 카메라 FOV 변화
                    ChangeFOV(PlayerAimState.hipFire);
                }
                // 정조준 상태
                else
                {
                    // 카메라 FOV 변화
                    postureFOV[(int)PlayerAimState.hipFire] = 60;
                    ChangeFOV(PlayerAimState.ads);
                }
                break;
            // 걷기 상태
            case PlayerPosture.walk:

                // 지향사격 상태
                if(playerAimState == PlayerAimState.hipFire)
                {
                    // 속도 변경
                    speed = walkSpeed;

                    // 카메라 FOV 변화
                    postureFOV[(int)PlayerAimState.hipFire] = 60;
                    ChangeFOV(PlayerAimState.hipFire);
                }
                // 정조준 상태
                else
                {
                    // 속도 변경
                    speed = walkSpeed * 0.5f;

                    // 카메라 FOV 변화
                    ChangeFOV(PlayerAimState.ads);
                }
                break;

            // 달리기 상태 
            case PlayerPosture.sprint:

                // 속도 변경
                speed = sprintSpeed;

                // 카메라 FOV 변화
                postureFOV[(int)PlayerAimState.hipFire] = 65;
                ChangeFOV(PlayerAimState.hipFire);
                break;

            // 앉기 상태
            case PlayerPosture.crouch:

                // 지향사격 상태
                if (playerAimState == PlayerAimState.hipFire)
                {
                    // 속도 변경
                    speed = crouchSpeed;

                    // 카메라 FOV 변화
                    postureFOV[(int)PlayerAimState.hipFire] = 60;
                    ChangeFOV(PlayerAimState.hipFire);

                }
                // 정조준 상태
                else
                {
                    // 속도 변경
                    speed = crouchSpeed * 0.5f;

                    // 카메라 FOV 변화
                    ChangeFOV(PlayerAimState.ads);
                }
                break;

            // 엎드리기 상태
            case PlayerPosture.prone:

                // 지향사격 상태
                if (playerAimState == PlayerAimState.hipFire)
                {
                    // 속도 변경
                    speed = proneSpeed;

                    // 카메라 FOV 변화
                    postureFOV[(int)PlayerAimState.hipFire] = 60;
                    ChangeFOV(PlayerAimState.hipFire);
                }
                // 정조준 상태
                else
                {
                    // 속도 변경
                    speed = proneSpeed * 0.5f;

                    // 카메라 FOV 변화
                    ChangeFOV(PlayerAimState.ads);
                }
                break;
        }

        animCon.SetFloat("Speed", speed);
    }

    // 카메라 FOV 전환
    private void ChangeFOV(PlayerAimState playerAimStatePara)
    {
        mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, postureFOV[(int)playerAimStatePara], Time.deltaTime * 5f);
        weaponCam.fieldOfView = Mathf.Lerp(weaponCam.fieldOfView, postureFOV[(int)playerAimStatePara], Time.deltaTime * 5f);

        // FOV 전환시 감도도 비율에 맞게 전환
        // sens = postureFOV[(int)playerAimStatePara] / postureFOV[(int)PlayerPosture.sprint];
    }

    // 이동
    private void Move()
    {
        // 키보드 입력(정수 -1, 1을 입력 받음)
        if (playerAimState != PlayerAimState.ads || playerPosture != PlayerPosture.prone)
        {
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
        }

        // 이동 방향 벡터
        moveVec = (transform.forward * moveZ + transform.right * moveX).normalized;

        // 정조준이 아니거나, 엎드리기가 아니면 움직일 수 있음
        // 엎드린 상태에서 정조준 중이면 움직임 제한


        // Debug.Log(moveVec);


        /*// 엎드리기 상태
        if (playerPosture == PlayerPosture.prone)
        {
            // 입력이 있을 때
            if (moveVec.magnitude > 0)
            {
                if(playerAimState == PlayerAimState.hipFire)
                {
                    // 현재 엎드리면서 지향사격 상태로 이동 중
                    isProneOriginMove = true;
                }
                else
                {
                    isProneOriginMove = false;
                }
            }
            // 그외 모든 자세
            else
            {
                isProneOriginMove = false;
            }
        }
        else
        {
            isProneOriginMove = false;
        }

        // 현재 엎드리면서 지향사격 상태로 이동 중이면
        // 애초에 정조준 상태로는 못 움직임
        if (isProneOriginMove)
        {
            // 우클릭하면 정조준
            // isProneOriginMove = false
            if (Input.GetButtonDown("ADS"))
            {
                //playerAimState = PlayerAimState.ads;
                //isProneOriginMove = false;
            }
        }
        else
        {
            if (Input.GetAxisRaw("Vertical") != 0)
            {
                Debug.Log("Test");
                // playerAimState = PlayerAimState.hipFire;
            }
        }
*/
        // 이동 키를 입력하면
        if (moveVec != Vector3.zero)
        {
            // 이동 방향으로 힘을 가함
            rigid.AddForce(moveVec * speed * Time.fixedDeltaTime, ForceMode.Impulse);

            // 플레이어 자세 변경
            if(playerPosture == PlayerPosture.idle)
            {
                playerPosture = PlayerPosture.walk;

                // 걷기 애니메이션
                animCon.SetBool("isWalk", true);
            }
        }

        // 걷거나 달리기 자세에서 이동 키를 입력하지 않으면
        // 
        else
        {
            if(playerPosture == PlayerPosture.walk || playerPosture == PlayerPosture.sprint)
            {
                playerPosture = PlayerPosture.idle;

                // 걷기 애니메이션
                animCon.SetBool("isWalk", false);
                animCon.SetBool("isSprint", false);
            }
        }

        // 엎드리면서 이동할 때는 isProneMove가 true이다가 정조준을 클릭하면 false로 바뀌고 이동을 멈춤
        // 엎드린 상태에서 isProneMove가 true일 때만 정조준 해제
        // 이외에는 해제 ㄴㄴ

        // 엎드리기 자세에서 정조준을 하면 이동을 멈춤
        // 정조준 상태가 아니거나 엎드리기 상태가 아닐 때 이동 키를 누르면 이동
        if (playerAimState != PlayerAimState.ads || playerPosture != PlayerPosture.prone)
        {
            
        }
        /*else
        {
            moveX = 0f;
            moveZ = 0f;
        }

        if (playerAimState != PlayerAimState.ads || playerPosture != PlayerPosture.prone)
        {
            // 입력이 있을 때만
            if (moveVec.magnitude > 0)
            {
                // 이동 방향으로 힘을 가함
                rigid.AddForce(moveVec * speed * Time.deltaTime, ForceMode.Impulse);
            }
        }*/
    }

    void IsJump()
    {
        if (Input.GetButtonDown("Jump") && isGround)
        {
            isJump = true;
        }
    }

    // 점프
    private void Jump()
    {
        // 지면에 닿았을 때, 기본, 달리기 자세일 때만 점프 가능
        // Space 키 && 지면에 닿았을 때 
        // if (Input.GetButtonDown("Jump") && isGround)
        if (isJump)
        {
            isJump = false;
            // 걷거나 달리기 자세일 때
            if (playerPosture == PlayerPosture.idle || playerPosture == PlayerPosture.walk || playerPosture == PlayerPosture.sprint)
            {
                isGround = false;

                rigid.velocity = new Vector3(rigid.velocity.x, 0, rigid.velocity.z);

                // 윗 방향으로 힘을 가함
                rigid.AddForce(transform.up * jumpForce, ForceMode.Impulse);
            }

            // 달리기 자세 또는 걷기 자세에서 점프시 걷기로 전환
            //else if (playerPosture == PlayerPosture.sprint || playerPosture == PlayerPosture.crouch || playerPosture == PlayerPosture.prone)
            if (playerPosture == PlayerPosture.sprint || playerPosture == PlayerPosture.crouch || playerPosture == PlayerPosture.prone)
            {
                // 현재 플레이어의 콜라이더 변경
                playerCollider = PlayerCollider.originCollider;

                // 현재 플레이어의 자세 변경
                playerPosture = PlayerPosture.walk;

                // 현재 플레이어 콜라이더 컴포넌트 변경
                // ChangeCollider(playerCollider);
            }
        }
    }

    // 중력
    private void Gravity()
    {
        if(!isGround)
        {
            rigid.AddForce(-transform.up * gravityForce, ForceMode.Acceleration);
        }



        /*// Ray의 정보를 담을 RaycastHit 변수 선언
        RaycastHit rayHit;

        // 바닥으로 짧은 Ray를 발사해서 충돌하면
        if(Physics.Raycast(transform.position, -transform.up, out rayHit, 1.1f))
        {
            // Ray가 바닥에 닿으면
            if (rayHit.collider.tag == "Floor")
            {
                // 점프 가능
                isGround = true;
            }
            else
                isGround = false;
        }
        // Ray가 충돌하지 않으면(공중에 뜸)
        else
        {
            // 아래로 힘을 가함
            rigid.AddForce(-transform.up * gravityForce, ForceMode.Acceleration);
            isGround = false;
        }
        */

        // 지면 확인 Ray
        // Debug.DrawRay(transform.position, -transform.up * 1.1f, Color.cyan);
    }

    private void OnCollisionEnter(Collision collision)
    {
        isGround = true;
        // Debug.Log("착지 애니메이션 시작");
        // animCon.SetTrigger("isLanding");

        // StartCoroutine("CameraRecoil");

    }

    private IEnumerator CameraRecoil()
    {
        yield return null;
        weaponCam.transform.localPosition += Vector3.Lerp(Vector3.zero, Vector3.up * 5f, Time.deltaTime * 2f);
        yield return new WaitForSeconds(0.5f);
        weaponCam.transform.localPosition += Vector3.Lerp(Vector3.zero, Vector3.down * 2f, Time.deltaTime * 2f);
        yield return null;
    }

    private void OnCollisionStay(Collision collision)
    {
        isGround = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        isGround = false;
    }
}
