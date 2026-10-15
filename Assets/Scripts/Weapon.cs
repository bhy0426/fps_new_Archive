using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    public Player player;
    public GameObject bulletPrefab;
    public Transform bulletPos;

    public enum Type { HG, AR}; // 무기 타입 열거형 선언
    public Type type; // 무기 타입 열거형 변수
    public int curAmmo; // 현재 탄약
    public int maxAmmo; // 한 탄창에 들어가는 탄약
    public int totalAmmo; // 소지 탄약
    public int damage; // 대미지


    public float reloadTime; // 재장전 시간
    public float attackRate; // 연사 속도
    public float recoil; // 반동 값

    // 반동 벡터
    public Vector3 recoilVec;
    public Vector3 testVec;

    // 정조준 위치
    public Vector3 originPos;
    public Vector3 adsPos;

    // 테스트용 프리펩
    public GameObject bulletholePrefab;
    public ParticleSystem fireParticle;
    public GameObject emptyShellPrefab;
    public Transform emptyShellPos;



    public IEnumerator Fire(Transform mainCam)
    {
        // 반동 벡터
        recoilVec = Random.insideUnitSphere * recoil;

        RaycastHit rayHit;
        Vector3 dirVec;

        // 총알 생성
        //GameObject instanceBullet = Instantiate(bulletPrefab, bulletPos.position, bulletPos.rotation);

        // 총알 이펙트
        //Rigidbody rigidBullet = instanceBullet.GetComponent<Rigidbody>();
        // 총알 이펙트 반동 추가
        //dirVec = bulletPos.transform.forward + recoilVec;
        //rigidBullet.AddForce(dirVec.normalized * bullet.bulletSpeed, ForceMode.Impulse);

        // 발사 파티클 이펙트
        fireParticle.Play();

        // 탄피 배출
        GameObject instantEmptyShell = Instantiate(emptyShellPrefab, emptyShellPos.position, emptyShellPos.rotation);
        Rigidbody rigidEmptyShell = instantEmptyShell.GetComponent<Rigidbody>();
        dirVec = (emptyShellPos.transform.right + Random.insideUnitSphere * 0.5f) + (-emptyShellPos.transform.forward + Random.insideUnitSphere * 0.5f);
        //rigidEmptyShell.AddForce((emptyShellPos.transform.right * Random.Range(1.0f, 3.0f) + emptyShellPos.transform.up * Random.Range(0.5f, 2.0f)), ForceMode.Impulse);
        rigidEmptyShell.AddForce(dirVec.normalized * 3f, ForceMode.Impulse);
        rigidEmptyShell.AddRelativeTorque(new Vector3(Random.Range(10f, 30f), Random.Range(10f, 30f), Random.Range(10f, 30f)), ForceMode.Impulse);


        // 실제 적과 충돌하는 Ray
        // 총알 이펙트와 동일한 반동을 Ray에도 추가
        if (Physics.Raycast(mainCam.transform.position, mainCam.transform.forward + recoilVec, out rayHit, Mathf.Infinity))
        {
            // 맞은 위치 표시
            // 맞은 벽과 각도를 맞추기 위해 Quaternion.LookRotation(rayHit.normal)을 사용
            if(rayHit.collider.tag != "Enemy")
            {

            }
            GameObject bullethole = Instantiate(bulletholePrefab, rayHit.point, Quaternion.LookRotation(rayHit.normal));
            bullethole.transform.SetParent(rayHit.collider.transform, true);
            // bullethole.transform.localScale = Vector3.one;

            // Ray를 시각적으로 표시
            Debug.DrawRay(mainCam.transform.position, rayHit.point - mainCam.transform.position, Color.red, 2f);

            if (rayHit.collider != null)
            {
                // Ray에 닿은 적 HP 감소
                if (rayHit.collider.tag == "Enemy")
                {
                    // Hit 메소드 호출
                    rayHit.collider.gameObject.GetComponent<Enemy>().Hit(damage);
                }
            }
        }

        player.isFire = true;
        yield return new WaitForSeconds(attackRate);
        player.isFire = false;
    }

    public IEnumerator ReloadCoroutine()
    {
        // 재장전 애니메이션 재생
        player.isReload = true;
        player.animCon.SetBool("isReload", player.isReload);

        yield return new WaitForSeconds(reloadTime);

        // 재장전 애니메이션 재생
        player.isReload = false;
        player.animCon.SetBool("isReload", player.isReload);

        // 전체 탄약이 최대 탄약보다 많거나 같은 경우
        if (totalAmmo >= maxAmmo)
        {
            totalAmmo -= maxAmmo - curAmmo;
            curAmmo += maxAmmo - curAmmo;
        }
        else // 전체 탄약이 최대 탄약보다 적은 경우
        {
            // 전체 탄약이 0이 아닌 경우에만 장전 가능
            if (totalAmmo != 0)
            {
                curAmmo = totalAmmo;
                totalAmmo = 0;
            }
        }
    }
}
