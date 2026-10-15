using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    Enemy enemy;
    AudioSource audioSource;
    public Transform bulletPos;
    public GameObject bulletPrefab;
    public GameObject gunGroup;
    public Collider[] hitColliders;
    bool isShot = false;
    public Vector3 recoilVec;
    // 기존 총기 위치
    Vector3 originVec = new Vector3(0f, 0.15f, 0f);

    private void Awake()
    {
        enemy = GetComponent<Enemy>();
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        Debug.DrawRay(bulletPos.position, transform.forward * 20f, Color.cyan);

        // 총기 위치 회복
        gunGroup.transform.localPosition = Vector3.Lerp(gunGroup.transform.localPosition, originVec, Time.deltaTime * 10f);

        if (!isShot)
        {
            StartCoroutine(Attack());


            // gunGroup.transform.localPosition = originVec;
        }
        Detect();
    }

    IEnumerator Attack()
    {
        RaycastHit rayHit;
        int ran = Random.Range(7, 10);
        isShot = true;
        // Ray를 구 안에 무작위 위치로 발사
        //
        for (int i = 0; i < ran; i++)
        {
            if (Physics.Raycast(bulletPos.transform.position, bulletPos.transform.forward, out rayHit, Mathf.Infinity))
            {
                GameObject bullet = Instantiate(bulletPrefab, bulletPos.position, bulletPos.rotation);
                Rigidbody bulletRigid = bullet.GetComponent<Rigidbody>();
                Bullet bulletInfo = bullet.GetComponent<Bullet>();

                Vector3 dirVec = (rayHit.point + Random.insideUnitSphere * 2f) - bullet.transform.position;
                bulletRigid.AddForce(dirVec * bulletInfo.bulletSpeed, ForceMode.Impulse);
                audioSource.PlayOneShot(audioSource.clip);

                // 반동
                gunGroup.transform.localPosition = Vector3.Lerp(gunGroup.transform.localPosition, gunGroup.transform.localPosition + Random.insideUnitSphere * 1f, Time.deltaTime * 10f);
            }


            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(Random.Range(0.8f, 1f));
        isShot = false;

    }

    void Detect()
    {
        hitColliders = Physics.OverlapSphere(transform.position, 20f);

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.tag == "Player")
            {
                // enemy.target = hitCollider.transform;
                enemy.isAttack = true;
            }
        }
    }
}
