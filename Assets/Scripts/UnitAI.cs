using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    Player,
    Enemy,
    Neutral // optional if you want non-fighting dummies
}

public class UnitAI : MonoBehaviour
{
    [Header("Unit Info")]
    public string unitName;
    public int starLevel = 1;
    public Team team = Team.Player;

    [Header("Stats")]
    public float maxHealth = 100;
    public float currentHealth;
    public float attackDamage = 10;
    public float attackSpeed = 1.0f; // attacks per second
    public float armor = 0;
    public float attackRange = 1.5f;
    public float mana = 0f;
    public float maxMana = 50f;
    [HideInInspector] public float currentMana = 0f;
    [Header("Traits")]
    public List<Trait> traits = new List<Trait>();

    [Header("AI Settings")]
    public bool canMove = true;
    public bool canAttack = true;
    public bool isAlive = true;

    [Header("UI")]
    public GameObject unitUIPrefab;
    private UnitUI ui;
    private Queue<HexTile> currentPath = new Queue<HexTile>();

    private float attackCooldown = 0f;
    [HideInInspector] public Animator animator;
    public Transform currentTarget;

    [Header("Team Settings")]
    public int teamID = 0;   // 0 = Player team, 1 = Enemy team (expand later)
    public static event System.Action<UnitAI> OnAnyUnitDeath;
    public event System.Action<UnitState> OnStateChanged;
    private UnitState _currentState = UnitState.Bench;
    [HideInInspector] public HexTile currentTile;
    public UnitState currentState
    {
        get => _currentState;
        set
        {
            if (_currentState == value) return;   // only fire on change
            _currentState = value;
            OnStateChanged?.Invoke(_currentState);
        }
    }

    public enum UnitState
    {
        Bench,
        BoardIdle,
        Combat
    }
    public void SetState(UnitState newState)
    {
        if (currentState != newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentMana = 0f;

        if (unitUIPrefab != null)
        {
            GameObject uiObj = Instantiate(unitUIPrefab, transform);
            ui = uiObj.GetComponent<UnitUI>();
            ui.Init(transform, maxHealth, maxMana);
        }
    }


    private void Update()
    {
        if (!isAlive) return;
        if (currentState != UnitState.Combat) return;

        // ✅ Keep animator synced with attack speed
        if (animator) animator.SetFloat("AttackSpeed", attackSpeed);

        if (currentTarget != null)
            FaceTarget(currentTarget.position);

        attackCooldown -= Time.deltaTime;

        if (currentTarget == null || !currentTarget.GetComponent<UnitAI>().isAlive)
        {
            currentTarget = FindNearestEnemy();
        }

        if (currentTarget != null && canAttack)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.position);

            // ✅ If enemy is on an adjacent hex, force it "in range"
            if (currentTile != null && currentTarget.TryGetComponent<UnitAI>(out UnitAI enemy))
            {
                if (BoardManager.Instance.AreNeighbors(currentTile, enemy.currentTile))
                {
                    dist = attackRange; // trick the system → allows attacking
                }
            }

            if (dist <= attackRange)
            {
                if (attackCooldown <= 0f)
                {
                    Attack(currentTarget);
                    attackCooldown = 1f / attackSpeed;
                }
                if (animator) animator.SetBool("IsRunning", false);
            }
            else if (canMove)
            {
                MoveTowards(currentTarget.position);
            }
        }
    }

    private void Attack(Transform target)
    {
        FaceTarget(target.position);

        if (animator) animator.SetTrigger("AttackTrigger");

        // ✅ Deal damage immediately (like old script, no animation event required)
        if (target.TryGetComponent(out UnitAI enemy))
        {
            enemy.TakeDamage(attackDamage);
            GainMana(10);

            var ks = GetComponent<KillSwitchAbility>();
            if (ks != null) ks.OnAttack(enemy);
        }
    }



    private void FaceTarget(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        direction.y = 0f; // keep upright

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }


    // 🔹 Call this from the auto attack animation (Animation Event)
    public void DealAutoAttackDamage()
    {
        if (currentTarget != null && currentTarget.TryGetComponent(out UnitAI enemy))
        {
            enemy.TakeDamage(attackDamage);
            GainMana(10);

            // 🔑 Check if this unit has KillSwitchAbility
            var ks = GetComponent<KillSwitchAbility>();
            if (ks != null) ks.OnAttack(enemy);
        }
    }

    public void TakeDamage(float damage)
    {
        float reducedDamage = damage * (1f - (armor * 0.005f));
        currentHealth -= reducedDamage;
        GainMana(1);

        Debug.Log($"{unitName} took {reducedDamage} damage. HP: {currentHealth}/{maxHealth}");
        if (ui != null)
            ui.UpdateHealth(currentHealth);   // ✅ refresh bar

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ClearTile()
    {
        if (currentTile != null)
        {
            if (currentTile.occupyingUnit == this)
                currentTile.occupyingUnit = null;
            currentTile = null;
        }
    }

    private IEnumerator FadeAndDestroy(float fadeDuration = 1.5f)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        List<Material> materials = new List<Material>();

        // Create unique instances of all materials so we don’t modify shared ones
        foreach (var r in renderers)
        {
            foreach (var mat in r.materials)
            {
                materials.Add(mat);
            }
        }

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);

            foreach (var mat in materials)
            {
                if (mat.HasProperty("_Color"))
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }

            yield return null;
        }

        Destroy(gameObject); // fully gone after fade
    }

    private void Die()
    {
        isAlive = false;
        OnAnyUnitDeath?.Invoke(this);

        if (animator) animator.SetTrigger("DieTrigger");
        Debug.Log($"{unitName} has died!");

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        ClearTile();
        if (ui != null) ui.gameObject.SetActive(false);

        // 🔹 Start fade BEFORE disabling script
        StartCoroutine(FadeAndDestroy(2.5f));

        // Now disable Update loop
        this.enabled = false;
    }

    private void GainMana(float amount)
    {
        currentMana += amount;
        if (ui != null)
            ui.UpdateMana(currentMana);   // ✅ refresh bar

        if (currentMana >= maxMana)
        {
            CastAbility();
            currentMana = 0;
            if (ui != null)
                ui.UpdateMana(currentMana);   // reset bar
        }
    }


    private void CastAbility()
    {
        if (currentTarget != null)
            FaceTarget(currentTarget.transform.position);

        if (animator) animator.SetTrigger("AbilityTrigger");

        // ✅ Pass target directly to abilities
        foreach (var ability in GetComponents<MonoBehaviour>())
        {
            if (ability is IUnitAbility unitAbility)
            {
                unitAbility.Cast(currentTarget?.GetComponent<UnitAI>());
                return;
            }
        }
    }

    // ✅ Only one interface definition now
    public interface IUnitAbility
    {
        void Cast(UnitAI target);
    }

    private Transform FindNearestEnemy()
    {
        UnitAI[] allUnits = FindObjectsOfType<UnitAI>();
        Transform nearest = null;
        float minDist = Mathf.Infinity;

        foreach (var unit in allUnits)
        {
            if (unit == this || !unit.isAlive) continue;
            if (unit.team == this.team) continue; // ✅ don’t target allies

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = unit.transform;
            }
        }
        return nearest;
    }
    public UnitAI GetCurrentTarget()
    {
        return currentTarget != null ? currentTarget.GetComponent<UnitAI>() : null;
    }

    private void MoveTowards(Vector3 targetPos)
    {
        if (currentTile == null) return;

        // If we don't have a path or we reached the end, compute one
        if (currentPath.Count == 0)
        {
            HexTile goalTile = BoardManager.Instance.GetTileFromWorld(targetPos);

            if (goalTile != null)
            {
                var path = BoardManager.Instance.FindPath(currentTile, goalTile);

                if (path.Count > 1) // first is currentTile
                {
                    currentPath = new Queue<HexTile>(path);
                    currentPath.Dequeue(); // drop current tile
                }
            }
        }

        // Move toward next step
        if (currentPath.Count > 0)
        {
            HexTile nextTile = currentPath.Peek();
            Vector3 nextPos = nextTile.transform.position;

            // ✅ Keep Y locked to current height (flat board fix)
            nextPos.y = transform.position.y;

            transform.position = Vector3.MoveTowards(transform.position, nextPos, Time.deltaTime * 2f);

            if (Vector3.Distance(transform.position, nextPos) < 0.05f)
            {
                // Arrived → claim tile
                if (nextTile.TryClaim(this))
                {
                    if (currentTile != null && currentTile != nextTile)
                        currentTile.Free(this);

                    currentTile = nextTile;
                }

                currentPath.Dequeue();
            }

            if (animator) animator.SetBool("IsRunning", true);
        }
        else
        {
            if (animator) animator.SetBool("IsRunning", false);
        }
    }

}
