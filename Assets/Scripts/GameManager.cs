using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    // 싱글톤
    public static GameManager instance;

    public TextMeshProUGUI playerStateText;
    public TextMeshProUGUI ammoText;

    // 조준점
    public GameObject originDot;
    public RectTransform dotTopRect;
    public RectTransform dotBottomRect;
    public RectTransform dotRightRect;
    public RectTransform dotLeftRect;
    public GameObject adsDot;

    public float originDotPos;
    public float recoilDotPos;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 프레임 제한
        Application.targetFrameRate = 60;

        // 마우스를 화면 가운데 고정
        Cursor.lockState = CursorLockMode.Locked; 


        // 모니터 주사율에 따른 TimeStamp 조절
        var ratio = Screen.currentResolution.refreshRateRatio;

        // 물리 연산 주기를 (1/주사율)로 설정
        // 만약 ratio를 제대로 받아오지 못하면 60으로 고정
        // double actualRefreshRate = (ratio.denominator > 0) ? ratio.value : 60.0;
        // Time.fixedDeltaTime = (float)(1.0 / actualRefreshRate);
        // Time.fixedDeltaTime = 1 / 120f;
        Time.fixedDeltaTime = 1 / 60f;

        // 프레임 제한도 주사율에 맞춤
        // Application.targetFrameRate = refreshRate;
    }
}
