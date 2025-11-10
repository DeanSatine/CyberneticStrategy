using UnityEngine;
using System.Collections;

[System.Serializable]
public class UnitVFXConfig
{
    [Header("Auto Attack VFX")]
    public GameObject autoAttackProjectile;
    public GameObject autoAttackMuzzleFlash;
    public GameObject autoAttackHitEffect;

    [Header("Ability VFX")]
    public GameObject abilityEffect;
    public GameObject abilityProjectile;
    public GameObject abilityImpactEffect;

    [Header("Audio")]
    public AudioClip autoAttackSound;
    public AudioClip abilitySound;
    [Range(0f, 1f)] public float volume = 0.7f;
}

public class CyberneticVFX : MonoBehaviour
{
    [Header("Unit-Specific VFX")]
    public UnitVFXConfig vfxConfig;

    [Header("Common Effects")]
    public GameObject deathVFX;
    public GameObject levelUpVFX;
    public GameObject damageNumbersPrefab;

    [Header("Fire Points")]
    public Transform firePoint; // For ranged units like NeedleBot
    public Transform chestPoint; // For B.O.P chest pound
    public Transform weaponPoint; // For melee attacks

    private UnitAI unitAI;
    private AudioSource audioSource;

    private void Awake()
    {
        unitAI = GetComponent<UnitAI>();
        audioSource = GetComponent<AudioSource>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.8f; // 3D spatial sound
        }

        // ✅ Auto-assign fire point from UnitAI if not set
        if (firePoint == null && unitAI.firePoint != null)
        {
            firePoint = unitAI.firePoint;
        }
    }

    private void Start()
    {
        // ✅ Subscribe to unit events
        if (unitAI != null)
        {
            unitAI.OnAttackEvent += OnAutoAttack;
            UnitAI.OnAnyUnitDeath += OnUnitDeath;
        }
    }

    private void OnDestroy()
    {
        if (unitAI != null)
        {
            unitAI.OnAttackEvent -= OnAutoAttack;
            UnitAI.OnAnyUnitDeath -= OnUnitDeath;
        }
    }

    // ✅ NEEDLEBOT: Auto attack VFX
    private void OnAutoAttack(UnitAI target)
    {
        if (unitAI.unitName.Contains("Needlebot"))
        {
            PlayNeedlebotAutoAttack(target);
        }
        else if (unitAI.unitName.Contains("B.O.P") || unitAI.unitName.Contains("BOP"))
        {
            PlayBOPAutoAttack(target);
        }
        else if (unitAI.unitName.Contains("ManaDrive"))
        {
            PlayManaDriveAutoAttack(target);
        }
        else if (unitAI.unitName.Contains("KillSwitch"))
        {
            PlayKillSwitchAutoAttack(target);
        }
        else if (unitAI.unitName.Contains("Haymaker"))
        {
            PlayHaymakerAutoAttack(target);
        }
        else
        {
            // ✅ Generic auto attack
            PlayGenericAutoAttack(target);
        }
    }

    // ✅ NEEDLEBOT: Fires needles from hand
    private void PlayNeedlebotAutoAttack(UnitAI target)
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;

        // ✅ Muzzle flash
        if (vfxConfig != null && vfxConfig.autoAttackMuzzleFlash != null)
        {
            PlayEffect(vfxConfig.autoAttackMuzzleFlash, spawnPos, 0.5f);
        }

        // ✅ Fire needle projectile with defensive checks
        if (target != null)
        {
            if (vfxConfig != null && vfxConfig.autoAttackProjectile != null)
            {
                StartCoroutine(FireNeedle(spawnPos, target));
            }
            else
            {
                // FALLBACK: Use unitAI projectile if VFX config missing
                Debug.LogWarning($"⚠️ Needlebot {unitAI.unitName} missing VFX config projectile - using unitAI.projectilePrefab");
                if (unitAI.projectilePrefab != null)
                {
                    StartCoroutine(FireNeedleFromUnitAI(spawnPos, target));
                }
                else
                {
                    Debug.LogError($"❌ Needlebot {unitAI.unitName} has NO projectile assigned! Cannot fire.");
                }
            }
        }

        if (vfxConfig != null)
        {
            PlaySound(vfxConfig.autoAttackSound);
        }
    }

    // ✅ NEEDLEBOT: Rapid throw ability (4 needles to nearest 2 enemies)
    public void PlayNeedlebotRapidThrow()
    {
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;

        // ✅ Ability effect at fire point
        if (vfxConfig != null && vfxConfig.abilityEffect != null)
        {
            PlayEffect(vfxConfig.abilityEffect, spawnPos, 1f);
        }

        // ✅ Find nearest 2 enemies
        UnitAI[] enemies = FindNearestEnemies(2);

        if (enemies.Length == 0)
        {
            Debug.LogWarning($"⚠️ Needlebot {unitAI.unitName} found no enemies for ability");
            return;
        }

        // ✅ Fire 4 needles split between them
        for (int i = 0; i < 4; i++)
        {
            UnitAI target = enemies[i % enemies.Length]; // Alternate between targets
            if (target != null)
            {
                if (vfxConfig != null && vfxConfig.autoAttackProjectile != null)
                {
                    StartCoroutine(FireNeedleWithDelay(spawnPos, target, i * 0.1f));
                }
                else if (unitAI.projectilePrefab != null)
                {
                    // FALLBACK: Use unitAI projectile
                    StartCoroutine(FireNeedleWithDelayFromUnitAI(spawnPos, target, i * 0.1f));
                }
            }
        }

        if (vfxConfig != null)
        {
            PlaySound(vfxConfig.abilitySound);
        }
    }

    // ✅ B.O.P: Chest pound + bonk
    private void PlayBOPAutoAttack(UnitAI target)
    {
        // ✅ Simple melee impact
        if (target != null && vfxConfig.autoAttackHitEffect != null)
        {
            PlayEffect(vfxConfig.autoAttackHitEffect, target.transform.position + Vector3.up, 1f);
        }

        PlaySound(vfxConfig.autoAttackSound);
    }

    public void PlayBOPChestPound()
    {
        Vector3 chestPos = chestPoint != null ? chestPoint.position : transform.position + Vector3.up * 1.2f;

        // ✅ Chest pound effect
        if (vfxConfig.abilityEffect != null)
        {
            PlayEffect(vfxConfig.abilityEffect, chestPos, 1f);
        }

        PlaySound(vfxConfig.abilitySound);
    }

    public void PlayBOPOverheadStrike(UnitAI target)
    {
        if (target != null && vfxConfig.abilityImpactEffect != null)
        {
            PlayEffect(vfxConfig.abilityImpactEffect, target.transform.position + Vector3.up, 1.5f);
        }
    }

    // ✅ MANADRIVE: Bomb throwing
    private void PlayManaDriveAutoAttack(UnitAI target)
    {
        if (vfxConfig.autoAttackProjectile != null && target != null)
        {
            StartCoroutine(FireBomb(target, false)); // Small bomb for auto attack
        }

        PlaySound(vfxConfig.autoAttackSound);
    }

    public void PlayManaDriveMassiveBomb(Vector3 targetPosition)
    {
        if (vfxConfig.abilityProjectile != null)
        {
            StartCoroutine(FireBombToPosition(targetPosition));
        }

        PlaySound(vfxConfig.abilitySound);
    }

    // ✅ KILLSWITCH: Melee attacks
    private void PlayKillSwitchAutoAttack(UnitAI target)
    {
        if (target != null && vfxConfig.autoAttackHitEffect != null)
        {
            PlayEffect(vfxConfig.autoAttackHitEffect, target.transform.position + Vector3.up, 0.8f);
        }

        PlaySound(vfxConfig.autoAttackSound);
    }

    public void PlayKillSwitchLeap()
    {
        // ✅ Leap trail effect
        if (vfxConfig.abilityEffect != null)
        {
            GameObject leapTrail = Instantiate(vfxConfig.abilityEffect, transform);
            Destroy(leapTrail, 2f);
        }

        PlaySound(vfxConfig.abilitySound);
    }

    public void PlayKillSwitchSlam(Vector3 position)
    {
        if (vfxConfig.abilityImpactEffect != null)
        {
            PlayEffect(vfxConfig.abilityImpactEffect, position, 1.5f);
        }
    }

    // ✅ HAYMAKER: Complex ability VFX
    private void PlayHaymakerAutoAttack(UnitAI target)
    {
        if (target != null && vfxConfig.autoAttackHitEffect != null)
        {
            PlayEffect(vfxConfig.autoAttackHitEffect, target.transform.position + Vector3.up, 1f);
        }

        PlaySound(vfxConfig.autoAttackSound);
    }

    public void PlayHaymakerStab(UnitAI target)
    {
        if (target != null && vfxConfig.abilityEffect != null)
        {
            // ✅ Stab effect between Haymaker and target
            Vector3 midPoint = Vector3.Lerp(transform.position, target.transform.position, 0.5f) + Vector3.up;
            PlayEffect(vfxConfig.abilityEffect, midPoint, 1f);
        }

        PlaySound(vfxConfig.abilitySound);
    }

    public void PlayHaymakerJumpSlam(Vector3 position, float radius = 2f)
    {
        if (vfxConfig.abilityImpactEffect != null)
        {
            // ✅ Scale effect based on radius
            GameObject slamEffect = Instantiate(vfxConfig.abilityImpactEffect, position, Quaternion.identity);
            slamEffect.transform.localScale = Vector3.one * radius;
            Destroy(slamEffect, 2f);
        }
    }

    // ✅ Generic auto attack for other units
    private void PlayGenericAutoAttack(UnitAI target)
    {
        if (vfxConfig.autoAttackProjectile != null && target != null)
        {
            // ✅ Ranged unit
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
            StartCoroutine(FireProjectile(spawnPos, target, vfxConfig.autoAttackProjectile, 15f));
        }
        else if (target != null && vfxConfig.autoAttackHitEffect != null)
        {
            // ✅ Melee unit
            PlayEffect(vfxConfig.autoAttackHitEffect, target.transform.position + Vector3.up, 1f);
        }

        PlaySound(vfxConfig.autoAttackSound);
    }

    // ✅ Death VFX
    private void OnUnitDeath(UnitAI deadUnit)
    {
        if (deadUnit == unitAI && deathVFX != null)
        {
            PlayEffect(deathVFX, transform.position + Vector3.up, 3f);
        }
    }

    // ✅ Damage numbers
    public void ShowDamageNumbers(float damage, Vector3 position)
    {
        if (damageNumbersPrefab != null)
        {
            GameObject numbers = Instantiate(damageNumbersPrefab, position + Vector3.up * 2f, Quaternion.identity);

            var textComponent = numbers.GetComponentInChildren<TMPro.TextMeshPro>();
            if (textComponent != null)
            {
                textComponent.text = Mathf.RoundToInt(damage).ToString();
                textComponent.color = Color.red;
            }

            Destroy(numbers, 2f);
        }
    }

    // ✅ HELPER METHODS

    private IEnumerator FireNeedle(Vector3 startPos, UnitAI target)
    {
        GameObject needle = Instantiate(vfxConfig.autoAttackProjectile, startPos, Quaternion.identity);

        while (needle != null && target != null && target.isAlive)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - needle.transform.position).normalized;

            needle.transform.position += direction * 20f * Time.deltaTime;
            needle.transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(needle.transform.position, targetPos) < 0.3f)
            {
                // ✅ Hit
                if (vfxConfig.autoAttackHitEffect != null)
                {
                    PlayEffect(vfxConfig.autoAttackHitEffect, targetPos, 0.5f);
                }

                Destroy(needle);
                yield break;
            }

            yield return null;
        }

        if (needle != null) Destroy(needle);
    }

    // FALLBACK: Fire needle using unitAI projectile if VFX config missing
    private IEnumerator FireNeedleFromUnitAI(Vector3 startPos, UnitAI target)
    {
        GameObject needle = Instantiate(unitAI.projectilePrefab, startPos, Quaternion.identity);

        while (needle != null && target != null && target.isAlive)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - needle.transform.position).normalized;

            needle.transform.position += direction * 20f * Time.deltaTime;
            needle.transform.rotation = Quaternion.LookRotation(direction);

            if (Vector3.Distance(needle.transform.position, targetPos) < 0.3f)
            {
                Destroy(needle);
                yield break;
            }

            yield return null;
        }

        if (needle != null) Destroy(needle);
    }

    private IEnumerator FireNeedleWithDelay(Vector3 startPos, UnitAI target, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(FireNeedle(startPos, target));
    }

    private IEnumerator FireNeedleWithDelayFromUnitAI(Vector3 startPos, UnitAI target, float delay)
    {
        yield return new WaitForSeconds(delay);
        yield return StartCoroutine(FireNeedleFromUnitAI(startPos, target));
    }

    private IEnumerator FireBomb(UnitAI target, bool isMassive)
    {
        Vector3 startPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        GameObject bomb = Instantiate(vfxConfig.autoAttackProjectile, startPos, Quaternion.identity);

        float speed = isMassive ? 8f : 12f;

        while (bomb != null && target != null && target.isAlive)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - bomb.transform.position).normalized;

            bomb.transform.position += direction * speed * Time.deltaTime;

            if (Vector3.Distance(bomb.transform.position, targetPos) < 0.5f)
            {
                // ✅ Bomb explodes
                if (vfxConfig.autoAttackHitEffect != null)
                {
                    GameObject explosion = Instantiate(vfxConfig.autoAttackHitEffect, targetPos, Quaternion.identity);
                    if (isMassive) explosion.transform.localScale *= 2f;
                    Destroy(explosion, 2f);
                }

                Destroy(bomb);
                yield break;
            }

            yield return null;
        }

        if (bomb != null) Destroy(bomb);
    }

    private IEnumerator FireBombToPosition(Vector3 targetPosition)
    {
        Vector3 startPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.5f;
        GameObject bomb = Instantiate(vfxConfig.abilityProjectile, startPos, Quaternion.identity);

        while (bomb != null)
        {
            Vector3 direction = (targetPosition - bomb.transform.position).normalized;
            bomb.transform.position += direction * 10f * Time.deltaTime;

            if (Vector3.Distance(bomb.transform.position, targetPosition) < 0.5f)
            {
                // ✅ Massive bomb explosion
                if (vfxConfig.abilityImpactEffect != null)
                {
                    GameObject explosion = Instantiate(vfxConfig.abilityImpactEffect, targetPosition, Quaternion.identity);
                    explosion.transform.localScale *= 3f; // Large explosion
                    Destroy(explosion, 3f);
                }

                Destroy(bomb);
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator FireProjectile(Vector3 startPos, UnitAI target, GameObject projectilePrefab, float speed)
    {
        GameObject projectile = Instantiate(projectilePrefab, startPos, Quaternion.identity);

        while (projectile != null && target != null && target.isAlive)
        {
            Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;
            Vector3 direction = (targetPos - projectile.transform.position).normalized;

            projectile.transform.position += direction * speed * Time.deltaTime;
            Quaternion baseRotation = projectilePrefab.transform.rotation; 
            projectile.transform.rotation = Quaternion.LookRotation(direction) * baseRotation;
            if (Vector3.Distance(projectile.transform.position, targetPos) < 0.3f)
            {
                if (vfxConfig.autoAttackHitEffect != null)
                {
                    PlayEffect(vfxConfig.autoAttackHitEffect, targetPos, 1f);
                }

                Destroy(projectile);
                yield break;
            }

            yield return null;
        }

        if (projectile != null) Destroy(projectile);
    }

    private UnitAI[] FindNearestEnemies(int count)
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        System.Collections.Generic.List<UnitAI> enemies = new System.Collections.Generic.List<UnitAI>();

        foreach (var unit in allUnits)
        {
            if (unit != unitAI && unit.isAlive && unit.team != unitAI.team)
            {
                enemies.Add(unit);
            }
        }

        enemies.Sort((a, b) => Vector3.Distance(transform.position, a.transform.position)
                              .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

        UnitAI[] result = new UnitAI[Mathf.Min(count, enemies.Count)];
        for (int i = 0; i < result.Length; i++)
        {
            result[i] = enemies[i];
        }

        return result;
    }

    private void PlayEffect(GameObject effectPrefab, Vector3 position, float duration)
    {
        if (effectPrefab != null)
        {
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            Destroy(effect, duration);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, vfxConfig.volume);
        }
    }
}
